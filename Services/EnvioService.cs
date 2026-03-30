using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServiceReference;
using System.Data;
using System.Globalization;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;

using static Tracker.Helpers.Constants;

namespace Tracker.Services;

public interface IEnvioService
{
    Task<List<Parametrico>> ObtenerEstadosEnvioAsync();
    Task<IEnumerable<EnvioDTO>> ObtenerEnviosAsync(FiltroEnvioDTO filtro);
    Task<EnvioDTO> ObtenerPorIdAsync(int id);
    Task<IEnumerable<GuiaDTO>> ObtenerGuiasAsync(FiltroEnvioDTO filtro, string? usuario);

    Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorGuiaAsync(FiltroEnvioDTO filtro, string? usuario);
    Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorGuiaAsync(Tracker_DevelContext context, FiltroEnvioDTO filtro, string? usuario);

    Task<MessageDTO> SincronizarAsync(Envio? envio, List<EnvioDTO>? listEnvios, UsuarioDTO usuario);
    Task<MessageDTO> PrepararEnvioASincronizarAsync(int envioId);
    Task<EnvioDTO?> ObtenerEnvioConDatosAsync(FiltroEnvioDTO filtroDTO);
    Task<MessageDTO> GuardarAsync(EnvioDTO envioDTO, int usuarioId);
}

public class EnvioService(Tracker_DevelContext context, IConfiguration configuration, IHubContext<NotificacionHub> notificationHubContext,
    IBackgroundTaskQueue backgroundTaskQueue,
    IServiceScopeFactory serviceScopeFactory,
    IEnvioAuditService envioAuditService,
    ILogger<EnvioService> logger) : IEnvioService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, byte> _syncInProgress = new();
    private readonly Tracker_DevelContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IHubContext<NotificacionHub> _notificationHubContext = notificationHubContext;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IEnvioAuditService _envioAuditService = envioAuditService;
    private readonly ILogger<EnvioService> _logger = logger;

    public async Task<IEnumerable<EnvioDTO>> ObtenerEnviosAsync(FiltroEnvioDTO filtro)
    {
        return await _context.EnvioDTO.FromSqlInterpolated($@"EXEC EnviosGet @desde = {filtro.Desde}, @hasta = {filtro.Hasta},
                                                                        @numero = {filtro.Numero}, @estado = {filtro.EstadoId},
                                                                        @guiaNumero = {filtro.GuiaNumero ?? 0},
                                                                        @pageSize =  {filtro.PageSize}, @skip = {filtro.Skip}")
            .AsNoTracking()
            .IgnoreQueryFilters()
            .ToListAsync();
    }

    public async Task<EnvioDTO> ObtenerPorIdAsync(int id)
    {
        if (id <= 0)
            return new EnvioDTO
            {
                FechaInicio = DateTime.Now.Date,
                FechaTurno = DateTime.Now.Date
            };

        var filtro = new FiltroEnvioDTO { Numero = id };
        var listEnvios = await ObtenerEnviosAsync(filtro);

        var envioDTO = listEnvios.FirstOrDefault() ?? new EnvioDTO();

        if (envioDTO.FechaTurno == null)
            envioDTO.FechaTurno = DateTime.Now.Date;

        if (envioDTO.FechaInicio == null)
            envioDTO.FechaInicio = DateTime.Now.Date;

        return envioDTO;
    }

    public async Task<IEnumerable<GuiaDTO>> ObtenerGuiasAsync(FiltroEnvioDTO filtro, string? usuario)
    {
        return await ObtenerGuiasInternalAsync(_context, filtro, usuario);
    }

    private async Task<IEnumerable<GuiaDTO>> ObtenerGuiasInternalAsync(
    Tracker_DevelContext context,
    FiltroEnvioDTO filtro,
    string? usuario)
    {
        return await context.GuiaDTO
            .FromSqlInterpolated($@"EXEC GuiasGet 
                                @numeroEnvio = {filtro.Numero ?? 0},
                                @numeroGuia = {filtro.GuiaNumero ?? 0},
                                @pageSize = {filtro.PageSize},
                                @skip = {filtro.Skip}")
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<ArticuloDTO>> ObtenerArticulosPorGuiaInternalAsync(Tracker_DevelContext context, FiltroEnvioDTO filtro, string? usuario)
    {
        var articulos = await context.ArticuloDTO
            .FromSqlInterpolated($@"EXEC GetArticulosPorGuia 
                                @numeroGuia = {filtro.Numero},
                                @pageSize = {filtro.PageSize},
                                @skip = {filtro.Skip}")
            .AsNoTracking()
            .ToListAsync();

        if (!articulos.Any())
            return articulos;

        var numGuias = articulos
            .Select(x => x.NumeroGuia)
            .Distinct()
            .ToList();

        var telefonos = await GetTelefonosPorGuiaAsync(context, numGuias, usuario);

        foreach (var art in articulos)
        {
            var key = (art.NumeroGuia, art.ClienteCodigo, art.CabeceraComprobantesAfiliado);

            if (telefonos.TryGetValue(key, out var tel))
            {
                art.Telefono = tel.Telefono;
                art.TelefonoOrigen = tel.OrigenDescripcion;
                art.AfiliadoNombre = tel.AfiliadoNombre;
            }
        }

        return articulos;
    }
    public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorGuiaAsync(FiltroEnvioDTO filtro, string? usuario)
    {
        return await ObtenerArticulosPorGuiaInternalAsync(_context, filtro, usuario);
    }

    public async Task<IEnumerable<ArticuloDTO>> ObtenerArticulosPorGuiaAsync(Tracker_DevelContext context, FiltroEnvioDTO filtro, string? usuario)
    {
        return await ObtenerArticulosPorGuiaInternalAsync(context, filtro, usuario);
    }


    public async Task<MessageDTO> PrepararEnvioASincronizarAsync(int envioId)
    {

        if (envioId < 0)
            return MessageDTO.Error("El envío debe ser válido.");


        var notificacion = new NotificacionDTO()
        {
            Mensaje = "Preparando el envio para sincronización.",
            Usuario = "Tracker",
            TipoMensaje = eTipoMensaje.Ok
        };

        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);


        int[] estadosValidos = [(int)eEnviosEstados.Pendiente, (int)eEnviosEstados.ConError];


        var envio = await _context.Envios
            .Include(e => e.Estado)
            .Include(e => e.Chofer)
            .Include(e => e.Vehiculo)
            .ThenInclude(v => v.Tipo)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == envioId);


        if (envio == null)
            return MessageDTO.Error("El envío no existe.");

        if (envio.EstadoId != null && !estadosValidos.Contains(envio.EstadoId.Value))
        {

            var observacion = $"El envío no se puede sincronizar porque su estado <b>{envio?.Estado?.Descripcion ?? ""}</b> no lo permite.";
            notificacion = new NotificacionDTO()
            {
                Mensaje = observacion,
                Usuario = "Tracker",
                TipoMensaje = eTipoMensaje.Warninig
            };

            await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);
            return MessageDTO.Warning(observacion);
        }


        envio.Transportista = await _context.vwTransportistas.AsNoTracking().FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaCodigo);
        envio.TransportistaDestino = await _context.vwTransportistas.AsNoTracking().FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaDestinoCodigo);

        return new MessageDTO
        {
            Status = MessageDTO.Estatus.OK,
            TagObj = envio
        };
    }

    public async Task<EnvioDTO?> ObtenerEnvioConDatosAsync(FiltroEnvioDTO filtroDTO)
    {
        filtroDTO.GuiaNumero = null;
        filtroDTO.PageSize = int.MaxValue;
        filtroDTO.Skip = 0;

        var listGuias = await ObtenerEnviosAsync(filtroDTO);
        var guia = listGuias.FirstOrDefault();

        if (guia == null) return null;

        if (filtroDTO.TransportistaDestinoCodigo.HasValue)
        {
            var transportista = await _context.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == filtroDTO.TransportistaDestinoCodigo);

            if (transportista != null)
            {
                guia.TransportistaDestinoCodigo = transportista.Codigo;
                guia.TransportistaDestino = transportista.Nombre;
            }
        }

        return guia;
    }

    public async Task<MessageDTO> SincronizarAsync(Envio? envio, List<EnvioDTO>? listEnvios, UsuarioDTO usuario)
    {
        if (usuario == null || string.IsNullOrWhiteSpace(usuario.Nombre))
            return MessageDTO.Error("El usuario es obligatorio para sincronizar.");

        if (listEnvios == null && envio == null)
            return MessageDTO.Error("Debe informar un envío o una lista de envíos para sincronizar.");

        var notificacion = new NotificacionDTO()
        {
            Mensaje = "Preparando el envio para sincronización.",
            Usuario = "Tracker",
            TipoMensaje = eTipoMensaje.Ok
        };

        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);

        _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<Tracker_DevelContext>();


            try
            {
                var auditorias = new List<EnvioAudit>();

                if (listEnvios != null && listEnvios.Count > 0)
                {
                    var total = listEnvios.Count;
                    var procesadosOk = 0;



                    for (var index = 0; index < total; index++)
                    {
                        var item = listEnvios[index];



                        // LOCK por envío 
                        if (!_syncInProgress.TryAdd(item.Numero, 0))
                        {
                            await _notificationHubContext.Clients.Group("Notificacion")
                                .SendAsync("ReceiveNotificacion", new NotificacionDTO
                                {
                                    Mensaje = $"El envío Nº {item.Numero} ya está en proceso.",
                                    Usuario = usuario.Nombre,
                                    TipoMensaje = eTipoMensaje.Warninig
                                }, cancellationToken: token);

                            continue; // salta este envío
                        }

                        try
                        {

                            await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                            {
                                Mensaje = $"Sincronizando envío {index + 1}/{total}: Nº {item.Numero}.",
                                Usuario = usuario.Nombre,
                                TipoMensaje = eTipoMensaje.Ok
                            }, cancellationToken: token);

                            var envioToSend = Mappers.MapTo(item);

                            envioToSend.Transportista = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == item.TransportistaCodigo);
                            envioToSend.TransportistaDestino = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == item.TransportistaDestinoCodigo);
                            envioToSend.Vehiculo = await scopedContext.Vehiculos.Include(i => i.Tipo).FirstOrDefaultAsync(x => x.Id == item.VehiculoId);
                            envioToSend.Chofer = await scopedContext.Choferes.FirstOrDefaultAsync(x => x.Id == item.ChoferId);

                            var result = await EnviarConAgrupacionPorTelefonoAsync(scopedContext, envioToSend, usuario, auditorias);

                            if (result.IsOk)
                            {
                                procesadosOk++;
                            }
                            else
                            {
                                await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                                {
                                    Mensaje = $"Error al sincronizar envío Nº {item.Numero}: {result.Value}",
                                    Usuario = usuario.Nombre,
                                    TipoMensaje = eTipoMensaje.Error
                                }, cancellationToken: token);
                            }

                        }
                        finally
                        {
                            //  LIBERA LOCK SIEMPRE
                            _syncInProgress.TryRemove(item.Numero, out _);
                        }
                    }

                    await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                    {
                        Mensaje = $"Resumen sincronización masiva: {procesadosOk}/{total} envíos procesados correctamente.",
                        Usuario = usuario.Nombre,
                        TipoMensaje = procesadosOk == total ? eTipoMensaje.Ok : eTipoMensaje.Warninig
                    }, cancellationToken: token);

                }
                else
                {
                    if (envio is null)
                    {
                        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                        {
                            Mensaje = "No se pudo sincronizar: envío nulo.",
                            Usuario = usuario.Nombre,
                            TipoMensaje = eTipoMensaje.Error
                        }, cancellationToken: token);
                        return;
                    }

                    if (!_syncInProgress.TryAdd(envio.Numero, 0))
                    {
                        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                        {
                            Mensaje = $"El envío Nº {envio.Numero} ya está en proceso de sincronización.",
                            Usuario = usuario.Nombre,
                            TipoMensaje = eTipoMensaje.Warninig
                        }, cancellationToken: token);
                        return;
                    }

                    try
                    {
                        //envio.Transportista = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaCodigo);
                        //envio.TransportistaDestino = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaDestinoCodigo);
                        //envio.Vehiculo = await scopedContext.Vehiculos.Include(i => i.Tipo).FirstOrDefaultAsync(x => x.Id == envio.VehiculoId);
                        //envio.Chofer = await scopedContext.Choferes.FirstOrDefaultAsync(x => x.Id == envio.ChoferId);

                        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
                        {
                            Mensaje = $"Sincronizando envío Nº {envio.Numero}.",
                            Usuario = usuario.Nombre,
                            TipoMensaje = eTipoMensaje.Ok
                        }, cancellationToken: token);

                        await EnviarConAgrupacionPorTelefonoAsync(scopedContext, envio, usuario, auditorias);
                    }
                    finally
                    {
                        _syncInProgress.TryRemove(envio.Numero, out _);
                    }
                }

                var notificacion = new NotificacionDTO()
                {
                    Mensaje = $"Sincronización Finalizada. {DateTime.Now:dd/MM/yyyy HH:mm:ss} ",
                    Usuario = usuario.Nombre,
                    TipoMensaje = eTipoMensaje.Ok
                };

                await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion, cancellationToken: token);
            }
            catch (Exception ex)
            {
                Error.WriteLog(ex);
            }
        });



        return MessageDTO.Ok("Se inició la sincronización en segundo plano.");
    }



    private async Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaInternalAsync(Tracker_DevelContext context, IEnumerable<long> numGuias, string? usuario)
    {
        var numGuiasCsv = string.Join(",", numGuias);

        var telefonos = await context.Set<TelefonoGuiaResultado>()
            .FromSqlRaw("EXEC dbo.GetTelefonosGuias @NumGuiasCSV = {0}", numGuiasCsv)
            .AsNoTracking()
            .ToListAsync();

        var dic = new Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>();
        var logEntries = new List<TelefonoGuiaLog>();

        foreach (var t in telefonos)
        {
            if (!Enum.TryParse<eTelefonoTablaOrigen>(t.TelefonoEstado, true, out var origen))
                continue;

            var key = (t.NumGuia, t.Cliente, t.Afiliado);

            switch (origen)
            {
                case eTelefonoTablaOrigen.DOMICILI
                    when !string.IsNullOrWhiteSpace(t.TelefonoDomicili):

                    dic[key] = new TelefonoConOrigenDTO
                    {
                        Telefono = t.TelefonoDomicili.Trim(),
                        Origen = origen,
                        AfiliadoNombre = t.AfiliadoNombre,
                        AfiliadoId = t.Afiliado
                    };
                    break;

                case eTelefonoTablaOrigen.AFILIADO
                    when !string.IsNullOrWhiteSpace(t.TelefonoAfiliado):

                    dic[key] = new TelefonoConOrigenDTO
                    {
                        Telefono = t.TelefonoAfiliado.Trim(),
                        Origen = origen,
                        AfiliadoNombre = t.AfiliadoNombre,
                        AfiliadoId = t.Afiliado
                    };
                    break;

                case eTelefonoTablaOrigen.AFILIADO_MULTIPLES_TEL:

                    dic[key] = new TelefonoConOrigenDTO
                    {
                        Telefono = "ERROR",
                        Origen = origen,
                        AfiliadoNombre = t.AfiliadoNombre,
                        AfiliadoId = t.Afiliado
                    };

                    logEntries.Add(new TelefonoGuiaLog
                    {
                        NumGuia = t.NumGuia,
                        Cliente = t.Cliente,
                        Afiliado = t.Afiliado,
                        Listapre = t.Listapre,
                        FechaRegistro = DateTime.Now,
                        TelefonoEstado = t.TelefonoEstado,
                        UsuarioRegistra = usuario
                    });

                    break;

                case eTelefonoTablaOrigen.SIN_TELEFONO:
                default:
                    break;
            }
        }

        if (logEntries.Count > 0)
        {
            context.TelefonosGuiasLog.AddRange(logEntries);
            await context.SaveChangesAsync();
        }

        return dic;
    }




    private Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaAsync(Tracker_DevelContext context, IEnumerable<long> numGuias, string? usuario)
    {
        return GetTelefonosPorGuiaInternalAsync(context, numGuias, usuario);
    }


    public async Task<MessageDTO> GuardarAsync(EnvioDTO envioDTO, int usuarioId)
    {
        if (envioDTO == null)
            return MessageDTO.Error("El envío es nulo.");

        var envio = Mappers.MapTo(envioDTO);
        envio.UsuarioId = usuarioId;
        envio.UsuarioUltimoMovId = usuarioId;
        envio.FechaUltimoMov = DateTime.Now;

        if (!envio.EstadoId.HasValue)
            envio.EstadoId = (int)Constants.eEnviosEstados.Pendiente;

        //
        // Ahora el codigo de viaje es unico (es la ruta sino no envian el mensaje x WhatsApp)
        //
        if (!envio.CodigoViaje.HasValue)
        {
            envio.CodigoViaje = Guid.NewGuid();
        }

        if (envioDTO.EnvioId == null || envioDTO.EnvioId == 0)
        {
            envio.EstadoId = (int)Constants.eEnviosEstados.Pendiente;
            _context.Envios.Attach(envio);
        }
        else
        {
            _context.Envios.Update(envio);
        }

        await _context.SaveChangesAsync();

        return new MessageDTO
        {
            Status = MessageDTO.Estatus.OK,
            TagId = envio.Id,
            Value = "Envío guardado con éxito"
        };

    }


    public async Task<List<Parametrico>> ObtenerEstadosEnvioAsync()
    {
        return await _context.Parametricos
            .Where(w => !w.Baja && w.ParametricosHeaderId == (int)eParametricosHeader.EnvioEstado)
            .OrderBy(o => o.Orden)
            .ToListAsync();
    }

    private async Task<MessageDTO> EnviarConAgrupacionPorTelefonoAsync(
        Tracker_DevelContext context,
        Envio envio,
        UsuarioDTO usuario,
         List<EnvioAudit> auditorias)
    {
        long? nroGuia = 0L;

        if (envio.Guias != null && envio.Guias.Count == 1)
            nroGuia = envio.Guias.FirstOrDefault()?.Numero ?? 0L;

        var filtro = new FiltroEnvioDTO
        {
            Numero = envio.Numero,
            GuiaNumero = nroGuia,
            PageSize = int.MaxValue,
            Skip = 0
        };

        var guias = (await ObtenerGuiasInternalAsync(context, filtro, usuario.Nombre)).ToList();

        if (!guias.Any())
            return await EnviarALogictrackerAsync(context, envio, usuario, auditorias);

        var articulos = new List<ArticuloDTO>();

        foreach (var guia in guias)
        {
            filtro.Numero = guia.Numero;
            filtro.GuiaNumero = null;

            var articulosGuia = await ObtenerArticulosPorGuiaInternalAsync(context, filtro, usuario.Nombre);

            var telefonosGuia = articulosGuia
                .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
                .Select(a => a.Telefono)
                .Distinct()
                .ToList();

            if (telefonosGuia.Count > 1)
            {
                Error.WriteLog($"WARN MULTIPLES TELEFONOS EN GUIA - Envio: {envio.Numero} Guia: {guia.Numero} Tels: {string.Join(",", telefonosGuia)}");


                //await _envioAuditService.AuditarEnvioAsync(new EnvioAudit
                //{
                //    Envio = envio.Numero,
                //    Guia = guia.Numero,
                //    Fecha = DateTime.Now,
                //    Usuario = usuario.Nombre,
                //    EstadoId = (int)eEnviosEstados.ConAdvertencias,
                //    Observacion = $"MULTIPLES TELEFONOS EN GUIA, SE ENVIA POR SEPARADO Tels: {string.Join(",", telefonosGuia)}"
                //});


                auditorias.Add(new EnvioAudit
                {
                    Envio = envio.Numero,
                    EstadoId = (int)eEnviosEstados.ConAdvertencias,
                    Fecha = DateTime.Now,
                    Guia = guia.Numero,
                    Usuario = usuario.Nombre,
                    Direccion = envio.TransportistaDestino?.Direccion,
                    CodigoViaje = envio.CodigoViaje,
                    Observacion = $"MULTIPLES TELEFONOS EN GUIA, SE ENVIA POR SEPARADO Tels: {string.Join(",", telefonosGuia)}",
                    Estado = null
                });


            }

            articulos.AddRange(articulosGuia);
        }

        var telefonos = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
            .Select(a => a.Telefono!.Trim())
            .Distinct()
            .ToList();

        if (telefonos.Count <= 1)
            return await EnviarALogictrackerAsync(context, envio, usuario, auditorias);

        MessageDTO? ultimoResultado = null;

        foreach (var telefono in telefonos)
        {
            var guiasNumeroTelefono = articulos
                .Where(a => string.Equals(a.Telefono?.Trim(), telefono, StringComparison.Ordinal))
                .Select(a => a.NumeroGuia)
                .Distinct()
                .ToHashSet();

            var guiasTelefono = guias
                .Where(g => guiasNumeroTelefono.Contains(g.Numero))
                .Select(g => new EnvioGuia
                {
                    Numero = g.Numero,
                    Fecha = g.Fecha,
                    EstadoId = g.EstadoId ?? (int)eEnviosEstados.Pendiente
                })
                .ToList();

            if (guiasTelefono.Count == 0)
                continue;

            var envioTelefono = ClonarEnvioParaTelefono(envio);
            envioTelefono.CodigoViaje = Guid.NewGuid();
            envioTelefono.TelefonoGrupo = telefono;

            foreach (var guiaTelefono in guiasTelefono)
                envioTelefono.Guias.Add(guiaTelefono);

            ultimoResultado = await EnviarALogictrackerAsync(context, envioTelefono, usuario, auditorias);
        }

        return ultimoResultado ?? MessageDTO.Warning("No se encontraron guías asociadas a teléfonos válidos para sincronizar.");
    }

    private Envio ClonarEnvioParaTelefono(Envio envio)
    {
        return new Envio
        {
            Id = envio.Id,
            Numero = envio.Numero,
            FechaInicio = envio.FechaInicio,
            FechaTurno = envio.FechaTurno,
            CodigoViaje = envio.CodigoViaje,
            Transportista = envio.Transportista,
            TransportistaDestino = envio.TransportistaDestino,
            Vehiculo = envio.Vehiculo,
            Chofer = envio.Chofer,
            UsuarioId = envio.UsuarioId,
            UsuarioUltimoMovId = envio.UsuarioUltimoMovId,
            Guias = []
        };
    }



    private async Task<MessageDTO> EnviarALogictrackerAsync(Tracker_DevelContext context, Envio? envio, UsuarioDTO usuario, List<EnvioAudit> auditorias)
    {
        try
        {
            if (envio == null)
                return MessageDTO.Error("El envio es un dato obligatorio");

            var wsSetting = _configuration.GetSection("Servicio").Get<WSSettingDTO>();
            if (wsSetting == null)
                return MessageDTO.Error("El servicio sin configuracion revise el appsetting");

            string url = wsSetting.URL ?? string.Empty;
            string prefijoTest = string.Empty;
            var servicioActivo = wsSetting.Activo;

            if (wsSetting.EntornoPruebas?.Activo ?? false)
            {
                url = wsSetting.EntornoPruebas.URL;
                prefijoTest = wsSetting.EntornoPruebas.Prefijo;
            }

            CrearDistribucionConEntidadesSoapClient? client = null;


            // Logging SOAP si está habilitado
            var logCfg = _configuration.GetSection("SoapLogging").Get<SoapLoggingOptions>() ?? new();

            if (servicioActivo)
            {
                client = new CrearDistribucionConEntidadesSoapClient(
                    CrearDistribucionConEntidadesSoapClient.EndpointConfiguration.CrearDistribucionConEntidadesSoap, url);

                if (logCfg.Enabled && !client.Endpoint.EndpointBehaviors.OfType<SoapLoggingBehavior>().Any())
                    client.Endpoint.EndpointBehaviors.Add(new SoapLoggingBehavior(_logger, logCfg.ToFile, logCfg.Path, logCfg.SimularSoap));
            }

            var request = new DistribucionConEntidadesWs
            {
                Empresa = wsSetting.Empresa,
                BaseOperativa = wsSetting.BaseOperativa,
                FechaInicio = envio.FechaInicio ?? DateTime.Now,
                FechaTurno = envio.FechaTurno ?? DateTime.Now,
            };

            //
            // Ahora el codigo de viaje es unico (es la ruta sino no envian el mensaje x WhatsApp)
            //
            if (!envio.CodigoViaje.HasValue)
                envio.CodigoViaje = Guid.NewGuid();

            //Le quito los - para que formen un numero largo de 32 caracteres
            request.CodigoViaje = envio.CodigoViaje?.ToString("N");

            // ---------------- TRANSPORTISTA ----------------

            if (envio.TransportistaDestino == null)
            {
                request.Transportista = new TransportistaWs
                {
                    Codigo = (envio?.Transportista?.Codigo != null)
                        ? string.Concat(prefijoTest, envio.Transportista.Codigo.ToString())
                        : "0",
                    Descripcion = (envio?.Transportista?.Nombre != null)
                        ? string.Concat(prefijoTest, envio.Transportista.Nombre)
                        : string.Empty,
                    Coordenadas = envio?.Transportista?.Coordenadas
                };
            }
            else
            {
                request.Transportista = new TransportistaWs
                {
                    Codigo = (envio?.TransportistaDestino?.Codigo != null)
                        ? string.Concat(prefijoTest, envio.TransportistaDestino.Codigo.ToString())
                        : "0",
                    Descripcion = string.Concat(prefijoTest, envio?.TransportistaDestino?.Nombre ?? string.Empty),
                    Coordenadas = envio?.TransportistaDestino?.Coordenadas
                };
            }

            // ---------------- CHOFER ----------------

            request.Chofer = new ChoferWs
            {
                Descripcion = string.Concat(prefijoTest, envio?.Chofer?.ApellidoNombre ?? string.Empty),
                Legajo = envio?.Chofer?.Legajo ?? string.Empty,
                Telefono = envio?.Chofer?.Telefono ?? string.Empty
            };

            // ---------------- VEHICULO ----------------

            request.Vehiculo = new VehiculoWs
            {
                Patente = envio?.Vehiculo?.Patente ?? string.Empty,
                TipoVehiculo = new TipoVehiculoWs
                {
                    Codigo = envio?.Vehiculo?.Tipo?.Codigo ?? string.Empty,
                    Descripcion = envio?.Vehiculo?.Tipo?.Descripcion ?? string.Empty
                }
            };

            // ---------------- OBTENER GUIAS ----------------

            long? nroGuia = 0L;

            if (envio?.Guias != null && envio.Guias.Count == 1)
                nroGuia = envio.Guias.FirstOrDefault()?.Numero ?? 0L;

            var filtro = new FiltroEnvioDTO
            {
                Numero = envio?.Numero,
                GuiaNumero = nroGuia,
                PageSize = int.MaxValue,
                Skip = 0
            };

            var listGuias = (await ObtenerGuiasInternalAsync(context, filtro, usuario.Nombre)).ToList();

            if (!listGuias.Any())
                return MessageDTO.Error("No se encontraron guías para el envío.");


            await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
            {
                Mensaje = $"finalizado de buscar las guias.",
                Usuario = usuario.Nombre,
                TipoMensaje = eTipoMensaje.Ok
            });


            // -----------------------------------------------------------
            // Traemos TODOS los artículos de todas las guías en una sola pasada
            // evitando ejecutar GetArticulosPorGuia por cada guía
            // -----------------------------------------------------------

            var articulosPorGuia = new Dictionary<long, List<ArticuloDTO>>();

            foreach (var guia in listGuias)
            {
                filtro.Numero = guia.Numero;

                var arts = await ObtenerArticulosPorGuiaInternalAsync(context, filtro, usuario.Nombre);

                articulosPorGuia[guia.Numero] = arts;
            }

            var envioSafe = envio;
            envioSafe.Estado = null;
            envioSafe.EstadoId = (int)eEnviosEstados.Correcto;

            var guiasNumero = listGuias.Select(g => g.Numero).Distinct().ToList();


            var guiasPersistidas = await context.EnviosGuias
                                        .Where(g => g.EnvioId == envioSafe.Id && guiasNumero.Contains(g.Numero))
                                        .GroupBy(g => g.Numero)
                                        .ToDictionaryAsync(g => g.Key, g => g.First());


            var huboErrores = false;

            // ---------------- PROCESAR GUIAS ----------------

            var totalGuias = listGuias.Count;
            var indexGuia = 0;


            foreach (var guia in listGuias)
            {
                var listClientes = new List<ClienteWs>();

                var articulos = articulosPorGuia.TryGetValue(guia.Numero, out var arts)
                                    ? arts
                                    : new List<ArticuloDTO>();

                if (!string.IsNullOrWhiteSpace(envio?.TelefonoGrupo))
                {
                    articulos = articulos
                        .Where(a => string.Equals(a.Telefono?.Trim(), envio.TelefonoGrupo.Trim(), StringComparison.Ordinal))
                        .ToList();
                }

                // ---------------- REMITOS ----------------

                var listRemitos = articulos
                    .GroupBy(a => a.NumeroComprobante)
                    .Select(groupRemito =>
                    {
                        var insumos = groupRemito
                            .GroupBy(a => a.ArticuloCodigo)
                            .Select(gInsumo => new InsumoCompletoWs
                            {
                                Codigo = gInsumo.Key.ToString(),
                                Descripcion = string.Concat(prefijoTest, gInsumo.First().ArticuloDescripcion ?? string.Empty),
                                Cantidad = gInsumo.Sum(x => (int)x.CantidadSolicitada)
                            })
                            .ToArray();

                        return new RemitoCompletoWs
                        {
                            Codigo = groupRemito.Key.ToString(CultureInfo.InvariantCulture),
                            Insumos = insumos
                        };
                    })
                    .ToList();


                // ---------------- TELEFONO ----------------

                var telefono = envio?.TelefonoGrupo;

                var afiliado = !string.IsNullOrWhiteSpace(envio?.TelefonoGrupo)
                                        ? articulos
                                            .Where(a => a.Telefono == envio.TelefonoGrupo)
                                            .Select(a => new
                                            {
                                                Id = a.CabeceraComprobantesAfiliado,
                                                Nombre = a.AfiliadoNombre
                                            })
                                            .FirstOrDefault()
                                        : null;

                var afiliadoNombre = afiliado?.Nombre ?? guia.AfiliadoNombre;

                var descripcionCliente = string.Concat(
                                            prefijoTest,
                                            guia.ClienteNombre ?? string.Empty,
                                            string.IsNullOrWhiteSpace(afiliadoNombre)
                                                ? string.Empty
                                                : string.Concat("–", afiliadoNombre)
                                            );


                var codigoCliente = (!string.IsNullOrWhiteSpace(envio?.TelefonoGrupo) && afiliado?.Id != null)
                                         ? $"{guia.ClienteCodigo}-{afiliado.Id}"
                                         : guia.ClienteAfiliado;


                listClientes.Add(new ClienteWs
                {
                    Codigo = codigoCliente,
                    Descripcion = descripcionCliente,
                    Coordenadas = envioSafe.TransportistaDestino?.Coordenadas ?? guia.Coordenadas,
                    Direccion = envioSafe.TransportistaDestino?.Direccion ?? guia.ClienteDireccion,
                    Telefono = telefono,
                    Remitos = listRemitos.ToArray()
                });

                request.Clientes = listClientes.ToArray();

                var guiaToBBDD = guiasPersistidas.TryGetValue(guia.Numero, out var guiaExistente)
                    ? guiaExistente
                    : new EnvioGuia
                    {
                        EnvioId = envioSafe.Id,
                        Fecha = guia.Fecha,
                        Numero = guia.Numero
                    };

                try
                {
                    GenericResponse resp;


                    // 🔥 SI EL CLIENT SE ROMPIÓ, LO RECREO
                    if (client != null && client.State == System.ServiceModel.CommunicationState.Faulted)
                    {
                        client.Abort();

                        client = new CrearDistribucionConEntidadesSoapClient(
                            CrearDistribucionConEntidadesSoapClient.EndpointConfiguration.CrearDistribucionConEntidadesSoap,
                            url);

                        if (logCfg.Enabled && !client.Endpoint.EndpointBehaviors.OfType<SoapLoggingBehavior>().Any())
                        {
                            client.Endpoint.EndpointBehaviors.Add(
                                new SoapLoggingBehavior(_logger, logCfg.ToFile, logCfg.Path, logCfg.SimularSoap));
                        }
                    }


                    // 2. Ejecutar normalmente
                    if (client == null)
                    {
                        resp = new GenericResponse { Codigo = 200 };
                    }
                    else
                    {
                        if (logCfg.SimularSoap)
                        {
                            // 🧪 MODO SIMULACIÓN
                            using (SoapLogContext.UseGuia(guia.Numero.ToString()))
                            {
                                try
                                {
                                    await client.CreateDistribucionConEntidadesAsync(request);
                                }
                                catch (Exception ex)
                                {
                                    Error.WriteLog($"SIMULACION SOAP ERROR (ignorado): {ex.Message}");
                                }
                            }
                            resp = new GenericResponse { Codigo = 200 };
                        }
                        else
                        {
                            //  MODO REAL
                            if (logCfg.Enabled && logCfg.ToFile)
                            {
                                using (SoapLogContext.UseGuia(guia.Numero.ToString()))
                                {
                                    var result = await client.CreateDistribucionConEntidadesAsync(request);
                                    resp = result?.Body?.CreateDistribucionConEntidadesResult
                                           ?? new GenericResponse { Codigo = 500, Mensaje = "Respuesta nula de LT" };
                                }
                            }
                            else
                            {
                                var result = await client.CreateDistribucionConEntidadesAsync(request);
                                resp = result?.Body?.CreateDistribucionConEntidadesResult
                                       ?? new GenericResponse { Codigo = 500, Mensaje = "Respuesta nula de LT" };
                            }
                        }

                    }



                    if (resp.Codigo == 200)
                    {
                        guiaToBBDD.EstadoId = (int)eEnviosEstados.Correcto;
                        // await RegistrarAuditoriaGuiaAsync(context, envioSafe, guia, usuario, telefono, (int)eEnviosEstados.Correcto, $"Logictracker OK ({resp.Codigo})");


                        auditorias.Add(new EnvioAudit
                        {
                            Envio = envioSafe.Numero,
                            EstadoId = (int)eEnviosEstados.Correcto,
                            Fecha = DateTime.Now,
                            Guia = guia.Numero,
                            Usuario = usuario.Nombre,
                            Direccion = envioSafe.TransportistaDestino?.Direccion,
                            CodigoViaje = envioSafe.CodigoViaje,
                            Observacion = $"{resp.Codigo}. Tel: {telefono ?? "N/A"}",
                            Estado = null
                        });

                    }
                    else
                    {
                        huboErrores = true;
                        guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;

                        // await RegistrarAuditoriaGuiaAsync(context, envioSafe, guia, usuario, telefono, (int)eEnviosEstados.ConError, $"Logictracker ERROR {resp.Codigo}: {resp.Mensaje}");

                        auditorias.Add(new EnvioAudit
                        {
                            Envio = envioSafe.Numero,
                            EstadoId = (int)eEnviosEstados.ConError,
                            Fecha = DateTime.Now,
                            Guia = guia.Numero,
                            Usuario = usuario.Nombre,
                            Direccion = envioSafe.TransportistaDestino?.Direccion,
                            CodigoViaje = envioSafe.CodigoViaje,
                            Observacion = $"Logictracker ERROR {resp.Codigo}: {resp.Mensaje}",
                            Estado = null
                        });

                        Error.WriteLog($"ERROR {resp.Codigo} - {resp.Mensaje} Envio: {envioSafe.Numero} Guia: {guia.Numero}");
                    }


                    // avance del proceso 
                    indexGuia++;

                    if (indexGuia == 1 || indexGuia % 10 == 0 || indexGuia == totalGuias)
                    {
                        await _notificationHubContext.Clients.Group("Notificacion")
                            .SendAsync("ReceiveNotificacion", new NotificacionDTO
                            {
                                Mensaje = $"Procesando guías {indexGuia}/{totalGuias} del envío {envioSafe.Numero}...",
                                Usuario = usuario.Nombre,
                                TipoMensaje = eTipoMensaje.Ok
                            });
                    }



                }
                catch (Exception exGuia)
                {
                    huboErrores = true;
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;

                    await RegistrarAuditoriaGuiaAsync(context, envioSafe, guia, usuario, telefono, (int)eEnviosEstados.ConError, $"EXCEPCION LT: {exGuia.Message}");



                    Error.WriteLog($"ERROR EXCEPCION LT Envio: {envioSafe.Numero} Guia: {guia.Numero} - {exGuia.Message}");
                }

                if (guiaExistente == null)
                {
                    //  Revalidación contra DB (evita duplicados por múltiples ejecuciones)
                    var existe = envioSafe.Id > 0 && await context.EnviosGuias
                        .AnyAsync(x => x.EnvioId == envioSafe.Id && x.Numero == guia.Numero);

                    if (!existe)
                    {
                        await context.EnviosGuias.AddAsync(guiaToBBDD);
                        guiasPersistidas[guia.Numero] = guiaToBBDD;
                    }
                }
            }


            if (envioSafe.Usuario == null)
            {
                envioSafe.UsuarioId = usuario.Id;
            }
            await ActualizarEstadoEnvioAsync(context, envioSafe, usuario, huboErrores);
            await context.SaveChangesAsync();

            await _envioAuditService.AuditarEnviosAsync(auditorias);


            return MessageDTO.Ok("Envío sincronizado con éxito.");
        }
        catch (Exception ex)
        {
            await _notificationHubContext.Clients.Group("Notificacion")
                .SendAsync("ReceiveNotificacion", new NotificacionDTO
                {
                    Mensaje = $"<span class='text-red'> ERROR {ex.Message} </span>",
                    Usuario = usuario.Nombre,
                    TipoMensaje = eTipoMensaje.Error
                });

            return MessageDTO.Error(ex.Message);
        }
    }





    private async Task RegistrarAuditoriaGuiaAsync(
    Tracker_DevelContext context,
    Envio envio,
    GuiaDTO guia,
    UsuarioDTO usuario,
    string? telefono,
    int estadoId,
    string resultado)
    {
        var telefonoUsado = string.IsNullOrWhiteSpace(telefono) ? "N/A" : telefono;

        var observacion = $"{resultado}. Tel: {telefonoUsado}. CodigoViaje: {envio.CodigoViaje}";

        var envioAudit = new EnvioAudit
        {
            Envio = envio.Numero,
            EstadoId = estadoId,
            Fecha = DateTime.Now,
            Guia = guia.Numero,
            Usuario = usuario.Nombre,
            Direccion = envio.TransportistaDestino?.Direccion,
            CodigoViaje = envio.CodigoViaje,
            Observacion = observacion
        };

        await _envioAuditService.AuditarEnvioAsync(envioAudit);
    }

    private readonly record struct TelefonoGuiaAuditInfo(
    string? Telefono,
    string Estado,
    string? AfiliadoNombre,
    long? AfiliadoId
);

    private readonly record struct TelefonoClienteItem(
    long Afiliado,
    string? Telefono,
    string ListaPrecio,
    string? AfiliadoNombre,
    string? OrigenTelefono)
    {
        public bool EsPrincipal =>
            !string.IsNullOrWhiteSpace(OrigenTelefono) &&
            OrigenTelefono.Contains("PRINCIPAL", StringComparison.OrdinalIgnoreCase);
    }


    private async Task ActualizarEstadoEnvioAsync(
     Tracker_DevelContext context,
     Envio envio,
     UsuarioDTO usuario,
     bool huboErrores)
    {
        var envioDb = await context.Envios
            .FirstOrDefaultAsync(x => x.Id == envio.Id);

        if (envioDb == null)
            return;

        envioDb.EstadoId = huboErrores
            ? (int)eEnviosEstados.ConError
            : (int)eEnviosEstados.Correcto;

        envioDb.FechaUltimoMov = DateTime.Now;
        envioDb.UsuarioUltimoMovId = usuario.Id;

        if (envioDb.UsuarioId == 0)
            envioDb.UsuarioId = usuario.Id;


    }
}
