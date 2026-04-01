using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

public class EnvioService(Tracker_DevelContext context,
    IConfiguration configuration,
    IHubContext<NotificacionHub> notificationHubContext,
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


    private async Task<List<ArticuloDTO>> ObtenerArticulosPorGuiasInternalAsync(
    Tracker_DevelContext context,
    IEnumerable<long> numGuias,
    string? usuario)
    {
        var numGuiasList = numGuias
            .Distinct()
            .ToList();

        if (!numGuiasList.Any())
            return [];

        var numGuiasCsv = string.Join(",", numGuiasList);

        var articulos = await context.ArticuloDTO
            .FromSqlRaw(
                @"EXEC GetArticulosPorGuias 
                @NumGuiasCsv = {0},
                @pageSize = {1},
                @skip = {2}",
                numGuiasCsv,
                int.MaxValue,
                0)
            .AsNoTracking()
            .ToListAsync();

        if (!articulos.Any())
            return articulos;

        var telefonos = await GetTelefonosPorGuiaAsync(context, numGuiasList, usuario);

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


    private async static Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaInternalAsync(
    Tracker_DevelContext context,
    IEnumerable<long> numGuias,
    string? usuario)
    {
        var numGuiasCsv = string.Join(",", numGuias);

        var telefonos = await context.Set<TelefonoGuiaResultado>()
            .FromSqlRaw("EXEC dbo.GetTelefonosGuias @NumGuiasCSV = {0}", numGuiasCsv)
            .AsNoTracking()
            .ToListAsync();

        var dic = new Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>();

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
                    break;

                case eTelefonoTablaOrigen.SIN_TELEFONO:
                default:
                    break;
            }
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
        var cantidadAuditoriasInicial = auditorias.Count;

        var enviosPreparados = await PrepararEnviosParaLogicTrackerAsync(context, envio, usuario, auditorias);

        if (!enviosPreparados.Any())
            return MessageDTO.Warning("No se encontraron datos para sincronizar.");

        MessageDTO? ultimoResultado = null;
        var huboErrores = false;

        foreach (var envioPreparado in enviosPreparados)
        {
            ultimoResultado = await EnviarEnvioPreparadoALogicTrackerAsync(context, envioPreparado, usuario, auditorias);

            if (ultimoResultado == null || !ultimoResultado.IsOk)
                huboErrores = true;
        }

        // Persistimos auditorías UNA sola vez por envío
        var auditoriasNuevas = auditorias
            .Skip(cantidadAuditoriasInicial)
            .ToList();

        if (auditoriasNuevas.Count > 0)
            await _envioAuditService.AuditarEnviosAsync(auditoriasNuevas);

        return ultimoResultado
            ?? (huboErrores
                ? MessageDTO.Warning("Envío sincronizado con observaciones o errores.")
                : MessageDTO.Ok("Envío sincronizado con éxito."));
    }

                                                
    private async Task<List<EnvioPreparadoDTO>> _PrepararEnviosParaLogicTrackerAsync(
    Tracker_DevelContext context,
    Envio envio,
    UsuarioDTO usuario,
    List<EnvioAudit> auditorias)
    {
        long? nroGuia = 0L;
        var logsSplitTelefonos = new List<TelefonoGuiaLog>();

        if (envio.Guias != null && envio.Guias.Count == 1)
            nroGuia = envio.Guias.FirstOrDefault()?.Numero ?? 0L;

        var filtro = new FiltroEnvioDTO
        {
            Numero = envio.Numero,
            GuiaNumero = nroGuia,
            PageSize = int.MaxValue,
            Skip = 0
        };

        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
        {
            Mensaje = "Buscando Guias",
            Usuario = usuario.Nombre,
            TipoMensaje = eTipoMensaje.Ok
        });

        var guias = (await ObtenerGuiasInternalAsync(context, filtro, usuario.Nombre)).ToList();

        if (!guias.Any())
            return [];

        // Se traen los artículos una sola vez para todas las guías del envío
        var numerosGuia = guias
            .Select(g => g.Numero)
            .Distinct()
            .ToList();

        var articulos = await ObtenerArticulosPorGuiasInternalAsync(context, numerosGuia, usuario.Nombre);

        if (!articulos.Any())
        {
            return
            [
                CrearEnvioPreparadoSinArticulos(envio, guias)
            ];
        }

        foreach (var guia in guias)
        {
            var articulosGuia = articulos
                .Where(a => a.NumeroGuia == guia.Numero)
                .ToList();

            var telefonosGuia = articulosGuia
                .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
                .Select(a => a.Telefono!.Trim())
                .Distinct()
                .ToList();

            if (telefonosGuia.Count > 1)
            {
                Error.WriteLog($"WARN MULTIPLES TELEFONOS EN GUIA - Envio: {envio.Numero} Guia: {guia.Numero} Tels: {string.Join(",", telefonosGuia)}");

                auditorias.Add(new EnvioAudit
                {
                    Envio = envio.Numero,
                    EstadoId = (int)eEnviosEstados.ConAdvertencias,
                    Fecha = DateTime.Now,
                    Guia = guia.Numero,
                    Usuario = usuario.Nombre,
                    Direccion = envio.TransportistaDestino?.Direccion,
                    CodigoViaje = envio.CodigoViaje,
                    Observacion = $"GUIA CON MULTIPLES TELEFONOS DETECTADOS: {string.Join(",", telefonosGuia)}",
                    Estado = null
                });
            }
        }

        var telefonosValidos = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
            .Select(a => a.Telefono!.Trim())
            .Distinct()
            .ToList();

        if (telefonosValidos.Count <= 1)
        {
            return
            [
                CrearEnvioPreparado(envio, guias, articulos, telefonosValidos.FirstOrDefault())
            ];
        }

        var enviosPreparados = new List<EnvioPreparadoDTO>();
        var splitsAuditados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var telefono in telefonosValidos)
        {
            var articulosTelefono = articulos
                .Where(a => string.Equals(a.Telefono?.Trim(), telefono, StringComparison.Ordinal))
                .ToList();

            if (!articulosTelefono.Any())
                continue;

            var guiasNumeroTelefono = articulosTelefono
                .Select(a => a.NumeroGuia)
                .Distinct()
                .ToHashSet();

            var guiasTelefono = guias
                .Where(g => guiasNumeroTelefono.Contains(g.Numero))
                .ToList();

            if (!guiasTelefono.Any())
                continue;
            
            

            foreach (var guiaTelefono in guiasTelefono)
            {
                var claveSplit = $"{envio.Numero}|{guiaTelefono.Numero}|{telefono}";

                if (!splitsAuditados.Add(claveSplit))
                    continue;

                //auditorias.Add(new EnvioAudit
                //{
                //    Envio = envio.Numero,
                //    EstadoId = (int)eEnviosEstados.ConAdvertencias,
                //    Fecha = DateTime.Now,
                //    Guia = guiaTelefono.Numero,
                //    Usuario = usuario.Nombre,
                //    Direccion = envio.TransportistaDestino?.Direccion,
                //    CodigoViaje = envio.CodigoViaje,
                //    Observacion = $"SPLIT POR TELEFONO - Guia: {guiaTelefono.Numero} - Tel: {telefono}",
                //    Estado = null
                //});
            }

            logsSplitTelefonos.AddRange(CrearAuditoriaSplitTelefonos(articulosTelefono, telefono, usuario.Nombre));


            enviosPreparados.Add(CrearEnvioPreparado(envio, guiasTelefono, articulosTelefono, telefono));
        }

        if (logsSplitTelefonos.Count > 0)
        {
            context.TelefonosGuiasLog.AddRange(logsSplitTelefonos);
            await context.SaveChangesAsync();
        }
        return enviosPreparados;
    }

    private async Task<List<EnvioPreparadoDTO>> PrepararEnviosParaLogicTrackerAsync(
    Tracker_DevelContext context,
    Envio envio,
    UsuarioDTO usuario,
    List<EnvioAudit> auditorias)
    {
        long? nroGuia = 0L;
        var logsSplitTelefonos = new List<TelefonoGuiaLog>();

        var wsSetting = _configuration.GetSection("Servicio").Get<WSSettingDTO>() ?? new WSSettingDTO();
        var usarSplitPorTelefono = wsSetting.HabilitarSplitPorTelefono;

        if (envio.Guias != null && envio.Guias.Count == 1)
            nroGuia = envio.Guias.FirstOrDefault()?.Numero ?? 0L;

        var filtro = new FiltroEnvioDTO
        {
            Numero = envio.Numero,
            GuiaNumero = nroGuia,
            PageSize = int.MaxValue,
            Skip = 0
        };

        await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", new NotificacionDTO
        {
            Mensaje = "Buscando Guias",
            Usuario = usuario.Nombre,
            TipoMensaje = eTipoMensaje.Ok
        });

        var guias = (await ObtenerGuiasInternalAsync(context, filtro, usuario.Nombre)).ToList();

        if (!guias.Any())
            return [];

        // Se traen todos los artículos de todas las guías del envío en una sola pasada
        var numerosGuia = guias
            .Select(g => g.Numero)
            .Distinct()
            .ToList();

        var articulos = await ObtenerArticulosPorGuiasInternalAsync(context, numerosGuia, usuario.Nombre);

        if (!articulos.Any())
        {
            return
            [
                CrearEnvioPreparadoSinArticulos(envio, guias)
            ];
        }

        // Detectamos guías con múltiples teléfonos válidos
        var guiasConMultiplesTelefonos = new HashSet<long>();

        foreach (var guia in guias)
        {
            var telefonosGuia = articulos
                .Where(a => a.NumeroGuia == guia.Numero &&
                            !string.IsNullOrWhiteSpace(a.Telefono) &&
                            a.Telefono != "ERROR")
                .Select(a => a.Telefono!.Trim())
                .Distinct()
                .ToList();

            if (telefonosGuia.Count > 1)
            {
                guiasConMultiplesTelefonos.Add(guia.Numero);

                Error.WriteLog($"WARN MULTIPLES TELEFONOS EN GUIA - Envio: {envio.Numero} Guia: {guia.Numero} Tels: {string.Join(",", telefonosGuia)}");

                auditorias.Add(new EnvioAudit
                {
                    Envio = envio.Numero,
                    EstadoId = (int)eEnviosEstados.ConAdvertencias,
                    Fecha = DateTime.Now,
                    Guia = guia.Numero,
                    Usuario = usuario.Nombre,
                    Direccion = envio.TransportistaDestino?.Direccion,
                    CodigoViaje = envio.CodigoViaje,
                    Observacion = usarSplitPorTelefono
                        ? $"GUIA CON MULTIPLES TELEFONOS DETECTADOS: {string.Join(",", telefonosGuia)}"
                        : $"GUIA CON MULTIPLES TELEFONOS DETECTADOS Y SPLIT DESHABILITADO. SE ENVIARA SIN TELEFONO. Tels: {string.Join(",", telefonosGuia)}",
                    Estado = null
                });
            }
        }

        var telefonosValidos = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
            .Select(a => a.Telefono!.Trim())
            .Distinct()
            .ToList();

        // Si el split está deshabilitado, nunca se separa el envío.
        // Las guías con múltiples teléfonos válidos se envían sin teléfono.
        if (!usarSplitPorTelefono)
        {
            foreach (var art in articulos)
            {
                if (guiasConMultiplesTelefonos.Contains(art.NumeroGuia))
                {
                    art.Telefono = null;
                    art.TelefonoOrigen = null;
                }
            }

            return
            [
                CrearEnvioPreparado(envio, guias, articulos, null)
            ];
        }

        // Si no hay split real para hacer, devolvemos un único envío
        if (telefonosValidos.Count <= 1)
        {
            return
            [
                CrearEnvioPreparado(envio, guias, articulos, telefonosValidos.FirstOrDefault())
            ];
        }

        // Split habilitado: se arma un envío por teléfono
        var enviosPreparados = new List<EnvioPreparadoDTO>();

        foreach (var telefono in telefonosValidos)
        {
            var articulosTelefono = articulos
                .Where(a => string.Equals(a.Telefono?.Trim(), telefono, StringComparison.Ordinal))
                .ToList();

            if (!articulosTelefono.Any())
                continue;

            var guiasNumeroTelefono = articulosTelefono
                .Select(a => a.NumeroGuia)
                .Distinct()
                .ToHashSet();

            var guiasTelefono = guias
                .Where(g => guiasNumeroTelefono.Contains(g.Numero))
                .ToList();

            if (!guiasTelefono.Any())
                continue;

            logsSplitTelefonos.AddRange(CrearAuditoriaSplitTelefonos(articulosTelefono, telefono, usuario.Nombre));

            enviosPreparados.Add(CrearEnvioPreparado(envio, guiasTelefono, articulosTelefono, telefono));
        }

        if (logsSplitTelefonos.Count > 0)
        {
            context.TelefonosGuiasLog.AddRange(logsSplitTelefonos);
            await context.SaveChangesAsync();
        }

        return enviosPreparados;
    }






    private static List<TelefonoGuiaLog> CrearAuditoriaSplitTelefonos(
    List<ArticuloDTO> articulosTelefono,
    string telefono,
    string? usuario)
    {
        return articulosTelefono
            .GroupBy(a => new
            {
                a.NumeroGuia,
                a.ClienteCodigo,
                a.CabeceraComprobantesAfiliado,
                a.ListaPrecio,
                a.AfiliadoNombre
            })
            .Select(g => new TelefonoGuiaLog
            {
                NumGuia = g.Key.NumeroGuia,
                Cliente = (int)g.Key.ClienteCodigo,
                Afiliado = g.Key.CabeceraComprobantesAfiliado,
                Listapre = g.Key.ListaPrecio ?? string.Empty,
                FechaRegistro = DateTime.Now,
                UsuarioRegistra = usuario,
                NombreAfiliado = g.Key.AfiliadoNombre,
                TelefonoEstado = $"SPLIT",
                Telefono = telefono
            })
            .ToList();
    }
    private static EnvioPreparadoDTO CrearEnvioPreparadoSinArticulos(
    Envio envio,
    List<GuiaDTO> guias)
    {
        return new EnvioPreparadoDTO
        {
            EnvioId = envio.Id,
            NumeroEnvio = envio.Numero,
            CodigoViaje = envio.CodigoViaje ?? Guid.NewGuid(),
            TelefonoGrupo = null,
            FechaInicio = envio.FechaInicio ?? DateTime.Now,
            FechaTurno = envio.FechaTurno ?? DateTime.Now,
            DireccionDestino = envio.TransportistaDestino?.Direccion,
            CoordenadasDestino = envio.TransportistaDestino?.Coordenadas,
            Transportista = envio.Transportista,
            TransportistaDestino = envio.TransportistaDestino,
            Chofer = envio.Chofer,
            Vehiculo = envio.Vehiculo,
            Guias = guias.Select(g => new GuiaPreparadaDTO
            {
                NumeroGuia = g.Numero,
                Fecha = g.Fecha,
                EstadoId = g.EstadoId ?? (int)eEnviosEstados.Pendiente,
                ClienteCodigo = g.ClienteCodigo,
                ClienteAfiliado = g.ClienteAfiliado,
                ClienteNombre = g.ClienteNombre,
                ClienteDireccion = g.ClienteDireccion,
                Coordenadas = g.Coordenadas,
                AfiliadoNombre = g.AfiliadoNombre,
                Telefono = null,
                Remitos = []
            }).ToList()
        };
    }

    private static EnvioPreparadoDTO CrearEnvioPreparado(
        Envio envio,
        List<GuiaDTO> guias,
        List<ArticuloDTO> articulosFiltrados,
        string? telefono)
    {
        var envioPreparado = new EnvioPreparadoDTO
        {
            EnvioId = envio.Id,
            NumeroEnvio = envio.Numero,
            // Si hay split por teléfono, cada paquete lleva un código de viaje propio
            CodigoViaje = string.IsNullOrWhiteSpace(telefono)
                ? (envio.CodigoViaje ?? Guid.NewGuid())
                : Guid.NewGuid(),
            TelefonoGrupo = telefono,
            FechaInicio = envio.FechaInicio ?? DateTime.Now,
            FechaTurno = envio.FechaTurno ?? DateTime.Now,
            DireccionDestino = envio.TransportistaDestino?.Direccion,
            CoordenadasDestino = envio.TransportistaDestino?.Coordenadas,
            Transportista = envio.Transportista,
            TransportistaDestino = envio.TransportistaDestino,
            Chofer = envio.Chofer,
            Vehiculo = envio.Vehiculo
        };

        foreach (var guia in guias)
        {
            var articulosGuia = articulosFiltrados
                .Where(a => a.NumeroGuia == guia.Numero)
                .ToList();

            // En split por teléfono, tomamos el afiliado real de los artículos filtrados
            var afiliado = !string.IsNullOrWhiteSpace(telefono)
                ? articulosGuia
                    .Select(a => new
                    {
                        Id = a.CabeceraComprobantesAfiliado,
                        Nombre = a.AfiliadoNombre
                    })
                    .FirstOrDefault()
                : null;

            // Acá se resuelve el bug principal:
            // los remitos salen SOLO de los artículos filtrados para ese teléfono
            var remitos = articulosGuia
                .GroupBy(a => a.NumeroComprobante)
                .Select(groupRemito => new RemitoPreparadoDTO
                {
                    NumeroRemito = groupRemito.Key,
                    Insumos = groupRemito
                        .GroupBy(x => x.ArticuloCodigo)
                        .Select(gInsumo => new InsumoPreparadoDTO
                        {
                            ArticuloCodigo = gInsumo.Key,
                            ArticuloDescripcion = gInsumo.First().ArticuloDescripcion,
                            Cantidad = gInsumo.Sum(x => (int)x.CantidadSolicitada)
                        })
                        .ToList()
                })
                .ToList();

            envioPreparado.Guias.Add(new GuiaPreparadaDTO
            {
                NumeroGuia = guia.Numero,
                Fecha = guia.Fecha,
                EstadoId = guia.EstadoId ?? (int)eEnviosEstados.Pendiente,
                ClienteCodigo = guia.ClienteCodigo,
                ClienteAfiliado = guia.ClienteAfiliado,
                ClienteNombre = guia.ClienteNombre,
                ClienteDireccion = guia.ClienteDireccion,
                Coordenadas = guia.Coordenadas,
                AfiliadoId = afiliado?.Id,
                AfiliadoNombre = afiliado?.Nombre ?? guia.AfiliadoNombre,
                Telefono = telefono,
                Remitos = remitos
            });
        }

        return envioPreparado;
    }

    private async Task<MessageDTO> EnviarEnvioPreparadoALogicTrackerAsync(
    Tracker_DevelContext context,
    EnvioPreparadoDTO envio,
    UsuarioDTO usuario,
    List<EnvioAudit> auditorias)
    {
        try
        {
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

            var logCfg = _configuration.GetSection("SoapLogging").Get<SoapLoggingOptions>() ?? new();

            if (servicioActivo)
            {
                client = new CrearDistribucionConEntidadesSoapClient(
                    CrearDistribucionConEntidadesSoapClient.EndpointConfiguration.CrearDistribucionConEntidadesSoap,
                    url);

                if (logCfg.Enabled && !client.Endpoint.EndpointBehaviors.OfType<SoapLoggingBehavior>().Any())
                {
                    client.Endpoint.EndpointBehaviors.Add(
                        new SoapLoggingBehavior(_logger, logCfg.ToFile, logCfg.Path, logCfg.SimularSoap));
                }
            }

            var request = ConstruirRequestLogicTracker(envio, wsSetting, prefijoTest);

            var guiasNumero = envio.Guias.Select(g => g.NumeroGuia).Distinct().ToList();

            var guiasPersistidas = await context.EnviosGuias
                .Where(g => g.EnvioId == envio.EnvioId && guiasNumero.Contains(g.Numero))
                .GroupBy(g => g.Numero)
                .ToDictionaryAsync(g => g.Key, g => g.First());

            var huboErrores = false;
            var totalGuias = envio.Guias.Count;
            var indexGuia = 0;

            foreach (var guia in envio.Guias)
            {
                request.Clientes =
                [
                    new ClienteWs
                {
                    Codigo = (!string.IsNullOrWhiteSpace(envio.TelefonoGrupo) && guia.AfiliadoId.HasValue)
                        ? $"{guia.ClienteCodigo}-{guia.AfiliadoId.Value}"
                        : guia.ClienteAfiliado,
                    Descripcion = string.Concat(
                        prefijoTest,
                        guia.ClienteNombre ?? string.Empty,
                        string.IsNullOrWhiteSpace(guia.AfiliadoNombre) ? string.Empty : string.Concat("–", guia.AfiliadoNombre)),
                    Coordenadas = envio.CoordenadasDestino ?? guia.Coordenadas,
                    Direccion = envio.DireccionDestino ?? guia.ClienteDireccion,
                    Telefono = guia.Telefono,
                    Remitos = guia.Remitos
                        .Select(r => new RemitoCompletoWs
                        {
                            Codigo = r.NumeroRemito.ToString(CultureInfo.InvariantCulture),
                            Insumos = r.Insumos
                                .Select(i => new InsumoCompletoWs
                                {
                                    Codigo = i.ArticuloCodigo.ToString(),
                                    Descripcion = string.Concat(prefijoTest, i.ArticuloDescripcion ?? string.Empty),
                                    Cantidad = i.Cantidad
                                })
                                .ToArray()
                        })
                        .ToArray()
                }
                ];

                var guiaToBBDD = guiasPersistidas.TryGetValue(guia.NumeroGuia, out var guiaExistente)
                    ? guiaExistente
                    : new EnvioGuia
                    {
                        EnvioId = envio.EnvioId,
                        Fecha = guia.Fecha ?? DateTime.Now,
                        Numero = guia.NumeroGuia
                    };

                try
                {
                    var resp = await EnviarRequestLogicTrackerAsync(client, request, guia.NumeroGuia, logCfg, url);

                    if (resp.Codigo == 200)
                    {
                        guiaToBBDD.EstadoId = (int)eEnviosEstados.Correcto;

                        auditorias.Add(new EnvioAudit
                        {
                            Envio = envio.NumeroEnvio,
                            EstadoId = (int)eEnviosEstados.Correcto,
                            Fecha = DateTime.Now,
                            Guia = guia.NumeroGuia,
                            Usuario = usuario.Nombre,
                            Direccion = envio.DireccionDestino,
                            CodigoViaje = envio.CodigoViaje,
                            Observacion = $"{resp.Codigo}. Tel: {guia.Telefono ?? "N/A"}",
                            Estado = null
                        });
                    }
                    else
                    {
                        huboErrores = true;
                        guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;

                        auditorias.Add(new EnvioAudit
                        {
                            Envio = envio.NumeroEnvio,
                            EstadoId = (int)eEnviosEstados.ConError,
                            Fecha = DateTime.Now,
                            Guia = guia.NumeroGuia,
                            Usuario = usuario.Nombre,
                            Direccion = envio.DireccionDestino,
                            CodigoViaje = envio.CodigoViaje,
                            Observacion = $"Logictracker ERROR {resp.Codigo}: {resp.Mensaje}. Tel: {guia.Telefono ?? "N/A"}",
                            Estado = null
                        });

                        Error.WriteLog($"ERROR {resp.Codigo} - {resp.Mensaje} Envio: {envio.NumeroEnvio} Guia: {guia.NumeroGuia}");
                    }

                    indexGuia++;

                    if (indexGuia == 1 || indexGuia % 10 == 0 || indexGuia == totalGuias)
                    {
                        await _notificationHubContext.Clients.Group("Notificacion")
                            .SendAsync("ReceiveNotificacion", new NotificacionDTO
                            {
                                Mensaje = $"Procesando guías {indexGuia}/{totalGuias} del envío {envio.NumeroEnvio}.",
                                Usuario = usuario.Nombre,
                                TipoMensaje = eTipoMensaje.Ok
                            });
                    }
                }
                catch (Exception exGuia)
                {
                    huboErrores = true;
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;

                    auditorias.Add(new EnvioAudit
                    {
                        Envio = envio.NumeroEnvio,
                        EstadoId = (int)eEnviosEstados.ConError,
                        Fecha = DateTime.Now,
                        Guia = guia.NumeroGuia,
                        Usuario = usuario.Nombre,
                        Direccion = envio.DireccionDestino,
                        CodigoViaje = envio.CodigoViaje,
                        Observacion = $"EXCEPCION LT: {exGuia.Message}. Tel: {guia.Telefono ?? "N/A"}",
                        Estado = null
                    });

                    Error.WriteLog($"ERROR EXCEPCION LT Envio: {envio.NumeroEnvio} Guia: {guia.NumeroGuia} - {exGuia.Message}");
                }

                if (guiaExistente == null)
                {
                    var existe = envio.EnvioId > 0 && await context.EnviosGuias
                        .AnyAsync(x => x.EnvioId == envio.EnvioId && x.Numero == guia.NumeroGuia);

                    if (!existe)
                    {
                        await context.EnviosGuias.AddAsync(guiaToBBDD);
                        guiasPersistidas[guia.NumeroGuia] = guiaToBBDD;
                    }
                }
            }

            var envioDb = await context.Envios.FirstOrDefaultAsync(x => x.Id == envio.EnvioId);
            if (envioDb != null)
            {
                envioDb.EstadoId = huboErrores
                    ? (int)eEnviosEstados.ConError
                    : (int)eEnviosEstados.Correcto;

                if (envioDb.UsuarioId == 0)
                    envioDb.UsuarioId = usuario.Id;
            }

            await context.SaveChangesAsync();


            return huboErrores
                    ? MessageDTO.Warning("Envío sincronizado con observaciones o errores.")
                    : MessageDTO.Ok("Envío sincronizado con éxito.");
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



    private static DistribucionConEntidadesWs ConstruirRequestLogicTracker(
    EnvioPreparadoDTO envio,
    WSSettingDTO wsSetting,
    string prefijoTest)
    {
        var request = new DistribucionConEntidadesWs
        {
            Empresa = wsSetting.Empresa,
            BaseOperativa = wsSetting.BaseOperativa,
            FechaInicio = envio.FechaInicio,
            FechaTurno = envio.FechaTurno,
            CodigoViaje = envio.CodigoViaje.ToString("N")
        };

        if (envio.TransportistaDestino == null)
        {
            request.Transportista = new TransportistaWs
            {
                Codigo = envio.Transportista?.Codigo != null
                    ? string.Concat(prefijoTest, envio.Transportista.Codigo.ToString())
                    : "0",
                Descripcion = envio.Transportista?.Nombre != null
                    ? string.Concat(prefijoTest, envio.Transportista.Nombre)
                    : string.Empty,
                Coordenadas = envio.Transportista?.Coordenadas
            };
        }
        else
        {
            request.Transportista = new TransportistaWs
            {
                Codigo = envio.TransportistaDestino.Codigo != null
                    ? string.Concat(prefijoTest, envio.TransportistaDestino.Codigo.ToString())
                    : "0",
                Descripcion = string.Concat(prefijoTest, envio.TransportistaDestino.Nombre ?? string.Empty),
                Coordenadas = envio.TransportistaDestino.Coordenadas
            };
        }

        request.Chofer = new ChoferWs
        {
            Descripcion = string.Concat(prefijoTest, envio.Chofer?.ApellidoNombre ?? string.Empty),
            Legajo = envio.Chofer?.Legajo ?? string.Empty,
            Telefono = envio.Chofer?.Telefono ?? string.Empty
        };

        request.Vehiculo = new VehiculoWs
        {
            Patente = envio.Vehiculo?.Patente ?? string.Empty,
            TipoVehiculo = new TipoVehiculoWs
            {
                Codigo = envio.Vehiculo?.Tipo?.Codigo ?? string.Empty,
                Descripcion = envio.Vehiculo?.Tipo?.Descripcion ?? string.Empty
            }
        };

        return request;
    }

    private async Task<GenericResponse> EnviarRequestLogicTrackerAsync(
        CrearDistribucionConEntidadesSoapClient? client,
        DistribucionConEntidadesWs request,
        long numeroGuia,
        SoapLoggingOptions logCfg,
        string url)
    {
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

        if (client == null)
            return new GenericResponse { Codigo = 200 };

        if (logCfg.SimularSoap)
        {
            using (SoapLogContext.UseGuia(numeroGuia.ToString()))
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

            return new GenericResponse { Codigo = 200 };
        }

        if (logCfg.Enabled && logCfg.ToFile)
        {
            using (SoapLogContext.UseGuia(numeroGuia.ToString()))
            {
                var result = await client.CreateDistribucionConEntidadesAsync(request);
                return result?.Body?.CreateDistribucionConEntidadesResult
                       ?? new GenericResponse { Codigo = 500, Mensaje = "Respuesta nula de LT" };
            }
        }

        {
            var result = await client.CreateDistribucionConEntidadesAsync(request);
            return result?.Body?.CreateDistribucionConEntidadesResult
                   ?? new GenericResponse { Codigo = 500, Mensaje = "Respuesta nula de LT" };
        }
    }

   

}