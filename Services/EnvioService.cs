using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    private async Task<List<ArticuloDTO>> ObtenerArticulosPorGuiaInternalAsync(    Tracker_DevelContext context,    FiltroEnvioDTO filtro,    string? usuario)
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

    public Task<MessageDTO> SincronizarAsync(Envio? envio, List<EnvioDTO>? listEnvios, UsuarioDTO usuario)
    {

        var notificacion = new NotificacionDTO()
        {
            Mensaje = "Preparando el envio para sincronización.",
            Usuario = "Tracker",
            TipoMensaje = eTipoMensaje.Ok
        };

        _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);

        _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<Tracker_DevelContext>();


            try
            {
                if (listEnvios != null && listEnvios.Count > 0)
                {
                    foreach (var item in listEnvios)
                    {

                        var envioToSend = Mappers.MapTo(item);

                        envioToSend.Transportista = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == item.TransportistaCodigo);
                        envioToSend.TransportistaDestino = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == item.TransportistaDestinoCodigo);
                        envioToSend.Vehiculo = await scopedContext.Vehiculos.Include(i => i.Tipo).FirstOrDefaultAsync(x => x.Id == item.VehiculoId);
                        envioToSend.Chofer = await scopedContext.Choferes.FirstOrDefaultAsync(x => x.Id == item.ChoferId);

                        await EnviarALogictrackerAsync(scopedContext, envioToSend, usuario);
                    }
                }
                else
                {

                    //envio.Transportista = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaCodigo);
                    //envio.TransportistaDestino = await scopedContext.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo == envio.TransportistaDestinoCodigo);
                    //envio.Vehiculo = await scopedContext.Vehiculos.Include(i => i.Tipo).FirstOrDefaultAsync(x => x.Id == envio.VehiculoId);
                    //envio.Chofer = await scopedContext.Choferes.FirstOrDefaultAsync(x => x.Id == envio.ChoferId);

                    await EnviarALogictrackerAsync(scopedContext, envio, usuario);
                }

                var notificacion = new NotificacionDTO()
                {
                    Mensaje = $"Sincronización Finalizada. {DateTime.Now:dd/MM/yyyy HH:mm:ss} ",
                    Usuario = "Tracker",
                    TipoMensaje = eTipoMensaje.Ok
                };

                await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion, cancellationToken: token);
            }
            catch (Exception ex)
            {
                Tracker.Helpers.Error.WriteLog(ex);

            }


        });



        return Task.FromResult(MessageDTO.Ok("Se inició la sincronización en segundo plano."));
    }

    //este se muere si le mandas 165 guias.
    //private async Task<Dictionary<long, TelefonoConOrigenDTO>> GetTelefonosPorGuiaAsyncOriginal(Tracker_DevelContext context, IEnumerable<long> numGuias, string? usuario)
    //{
    //    var telefonos = await context.vwTelefonosGuias
    //        .Where(t => numGuias.Contains(t.NumGuia))
    //        .AsNoTracking()
    //        .ToListAsync();

    //    var dic = new Dictionary<long, TelefonoConOrigenDTO>();
    //    var logEntries = new List<TelefonoGuiaLog>();

    //    foreach (var t in telefonos)
    //    {
    //        if (!Enum.TryParse<eTelefonoTablaOrigen>(t.TelefonoEstado, out var origen))
    //            continue;

    //        if (origen == eTelefonoTablaOrigen.DOMICILI && !string.IsNullOrWhiteSpace(t.TelefonoDomicili))
    //        {
    //            dic[t.NumGuia] = new TelefonoConOrigenDTO { Telefono = t.TelefonoDomicili, Origen = origen };
    //        }
    //        else if (origen == eTelefonoTablaOrigen.AFILIADO && !string.IsNullOrWhiteSpace(t.TelefonoAfiliado))
    //        {
    //            dic[t.NumGuia] = new TelefonoConOrigenDTO { Telefono = t.TelefonoAfiliado, Origen = origen };
    //        }
    //        else if (origen == eTelefonoTablaOrigen.AFILIADO_MULTIPLES)
    //        {
    //            logEntries.Add(new TelefonoGuiaLog
    //            {
    //                NumGuia = t.NumGuia,
    //                Cliente = t.Cliente,
    //                Afiliado = t.Afiliado,
    //                Listapre = t.Listapre,
    //                FechaRegistro = DateTime.Now,
    //                NombreAfiliado = t.NombreAfiliado,
    //                TelefonoEstado = t.TelefonoEstado,
    //                UsuarioRegistra = usuario
    //            });
    //        }
    //    }

    //    if (logEntries.Count > 0)
    //    {
    //        _context.TelefonosGuiasLog.AddRange(logEntries);
    //        await _context.SaveChangesAsync();
    //    }

    //    return dic;
    //}

    private async Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaInternalAsync(    Tracker_DevelContext context,    IEnumerable<long> numGuias,    string? usuario)
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
                        Origen = origen
                    };
                    break;

                case eTelefonoTablaOrigen.AFILIADO
                    when !string.IsNullOrWhiteSpace(t.TelefonoAfiliado):

                    dic[key] = new TelefonoConOrigenDTO
                    {
                        Telefono = t.TelefonoAfiliado.Trim(),
                        Origen = origen
                    };
                    break;

                case eTelefonoTablaOrigen.AFILIADO_MULTIPLES_TEL:

                    dic[key] = new TelefonoConOrigenDTO
                    {
                        Telefono = "ERROR",
                        Origen = origen
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


    private Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaAsync(IEnumerable<long> numGuias, string? usuario)
    {
        return GetTelefonosPorGuiaInternalAsync(_context, numGuias, usuario);
    }


    private Task<Dictionary<(long NumGuia, long Cliente, long Afiliado), TelefonoConOrigenDTO>> GetTelefonosPorGuiaAsync(Tracker_DevelContext context, IEnumerable<long> numGuias, string? usuario)
    {
        return GetTelefonosPorGuiaInternalAsync(context, numGuias, usuario);
    }


    private async Task<MessageDTO> zEnviarALogictrackerAsync(Tracker_DevelContext context, Envio? envio, UsuarioDTO usuario)
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

            if (wsSetting.EntornoPruebas?.Activo ?? false)
            {
                url = wsSetting.EntornoPruebas.URL;
                prefijoTest = wsSetting.EntornoPruebas.Prefijo;
            }

            using var client = new CrearDistribucionConEntidadesSoapClient(
                CrearDistribucionConEntidadesSoapClient.EndpointConfiguration.CrearDistribucionConEntidadesSoap, url);

            // Logging SOAP si está habilitado
            var logCfg = _configuration.GetSection("SoapLogging").Get<SoapLoggingOptions>() ?? new();
            if (logCfg.Enabled && !client.Endpoint.EndpointBehaviors.OfType<SoapLoggingBehavior>().Any())
            {
                client.Endpoint.EndpointBehaviors.Add(new SoapLoggingBehavior(_logger, logCfg.ToFile, logCfg.Path));
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
            {
                envio.CodigoViaje = Guid.NewGuid();
            }

            //Le quito los - para que formen un numero largo de 32 caracteres
            request.CodigoViaje = envio.CodigoViaje?.ToString("N");

            // --- Transportista ---
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

            // --- Chofer ---
            request.Chofer = new ChoferWs
            {
                Descripcion = string.Concat(prefijoTest, envio?.Chofer?.ApellidoNombre ?? string.Empty),
                Legajo = envio?.Chofer?.Legajo ?? string.Empty,
                Telefono = envio?.Chofer?.Telefono ?? string.Empty
            };

            // --- Vehículo ---
            request.Vehiculo = new VehiculoWs
            {
                Patente = envio?.Vehiculo?.Patente ?? string.Empty,
                TipoVehiculo = new TipoVehiculoWs
                {
                    Codigo = envio?.Vehiculo?.Tipo?.Codigo ?? string.Empty,
                    Descripcion = envio?.Vehiculo?.Tipo?.Descripcion ?? string.Empty
                }
            };

            // --- Busco las guías ---
            long? nroGuia = 0L;
            if (envio?.Guias != null && envio.Guias.Count == 1)
            {
                // si Numero es long?, coalesce a 0L
                nroGuia = envio.Guias.FirstOrDefault()?.Numero ?? 0L;
            }

            var filtro = new FiltroEnvioDTO
            {
                Numero = envio?.Numero,
                GuiaNumero = nroGuia,
                PageSize = int.MaxValue,
                Skip = 0
            };

            var listGuias = await ObtenerGuiasAsync(filtro, usuario.Nombre);
            var notificacion = new NotificacionDTO();

            // Estado por defecto OK (se ajusta si hay error)
            envio!.Estado = null;
            envio.EstadoId = (int)eEnviosEstados.Correcto;

            foreach (var guia in listGuias)
            {
                var listClientes = new List<ClienteWs>();

                // En algunos flujos usamos Numero como "contexto" para traer artículos de esa guía
                filtro.Numero = guia.Numero;

                //Ahora los telefonos estan aca.
                var articulos = await ObtenerArticulosPorGuiaAsync(filtro, usuario.Nombre) ?? new List<ArticuloDTO>();


                // === Consolidación: un Remito por NumeroComprobante con N Insumos (sumados por ArticuloCodigo) ===
                var listRemitos = articulos
                    // Si NumeroComprobante es long? => coalesco a 0L para la clave
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

                // === Código y Descripción del Cliente enriquecidos con afiliado ===
                // Codigo: {ClienteCodigo}-{ClienteAfiliado} si están ambos, o el que haya
                var clienteCodigoBase = Convert.ToString(guia.ClienteCodigo); // soporta nullables / distintos tipos

                // Descripcion: {prefijo}{ClienteNombre} – {ClienteAfiliado} (si hay afiliado)
                var descripcionCliente = string.Concat(
                    prefijoTest,
                    guia.ClienteNombre ?? string.Empty,
                    string.IsNullOrWhiteSpace(guia.ClienteAfiliado) ? string.Empty : string.Concat("–", guia.AfiliadoNombre)
                );


                // Detecta si hay múltiples teléfonos distintos
                var telefono = articulos
                    .Select(a => a.Telefono)
                    .Where(t => !string.IsNullOrWhiteSpace(t) && t != "ERROR")
                    .Distinct()
                    .SingleOrDefault();


                listClientes.Add(new ClienteWs
                {
                    Codigo = guia.ClienteAfiliado,
                    Descripcion = descripcionCliente,
                    Coordenadas = envio.TransportistaDestino?.Coordenadas ?? guia.Coordenadas,
                    Direccion = envio.TransportistaDestino?.Direccion ?? guia.ClienteDireccion,
                    Telefono = telefono,
                    Remitos = listRemitos.ToArray()
                });

                //
                request.Clientes = listClientes.ToArray();

                string guiaEstado = string.Empty;
                var guiaToBBDD = new EnvioGuia
                {
                    Fecha = guia.Fecha,
                    Numero = guia.Numero
                };

                GenericResponse resp;

                if (!wsSetting.Activo) // bypass WS
                {
                    resp = new GenericResponse { Codigo = 200 };
                }
                else
                {
                    // Logging por guía si aplica
                    if (logCfg.Enabled && logCfg.ToFile)
                    {
                        using (SoapLogContext.UseGuia(guia.Numero.ToString()))
                        {
                            var result = await client.CreateDistribucionConEntidadesAsync(request);
                            resp = result.Body.CreateDistribucionConEntidadesResult;
                        }
                    }
                    else
                    {
                        var result = await client.CreateDistribucionConEntidadesAsync(request);
                        resp = result.Body.CreateDistribucionConEntidadesResult;
                    }
                }

                // --- Respuesta WS ---
                if (resp.Codigo == 200)
                {
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.Correcto;
                    guiaEstado = $"<span class='text-green'> OK  <small> {DateTime.Now}</small>  </span>";

                    var envioAudit = new EnvioAudit
                    {
                        Envio = envio.Numero,
                        EstadoId = (int)eEnviosEstados.Correcto,
                        Fecha = DateTime.Now,
                        Guia = guia.Numero,
                        Usuario = usuario.Nombre,
                        Direccion = envio.TransportistaDestino?.Direccion,
                        CodigoViaje = envio.CodigoViaje
                    };

                    await _envioAuditService.AuditarEnvioAsync(context, envioAudit);

                    notificacion = new NotificacionDTO
                    {
                        Mensaje = $"Envio Nº {envio.Numero} Guia: {guia.Numero} estado {guiaEstado}",
                        Usuario = usuario.Nombre,
                        TipoMensaje = eTipoMensaje.Ok
                    };

                    await _notificationHubContext.Clients.Group("Notificacion")
                        .SendAsync("ReceiveNotificacion", notificacion);
                }
                else
                {
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;
                    envio.EstadoId = (int)eEnviosEstados.ConError;

                    guiaEstado = $"<span class='text-red'> E R R O R  {resp.Codigo} - {resp.Mensaje} <small> {DateTime.Now}</small>  </span>";

                    Error.WriteLog($"ERROR {resp.Codigo} - {resp.Mensaje} Envio: {envio.Numero} Guia: {guia.Numero} ");

                    // A veces LT manda error por su propio log.txt; lo ignoramos
                    if (resp.Mensaje.IndexOf("log.txt", StringComparison.OrdinalIgnoreCase) <= 0)
                    {
                        var envioAudit = new EnvioAudit
                        {
                            Envio = envio.Numero,
                            EstadoId = (int)eEnviosEstados.ConError,
                            Fecha = DateTime.Now,
                            Guia = guia.Numero,
                            Usuario = usuario.Nombre,
                            Observacion = guiaEstado,
                            CodigoViaje = envio.CodigoViaje
                        };

                        await _envioAuditService.AuditarEnvioAsync(context, envioAudit);

                        notificacion = new NotificacionDTO
                        {
                            Mensaje = $"Envio Nº {envio.Numero} Guia: {guia.Numero} estado {guiaEstado}",
                            Usuario = usuario.Nombre,
                            TipoMensaje = eTipoMensaje.Error
                        };

                        await _notificationHubContext.Clients.Group("Notificacion")
                            .SendAsync("ReceiveNotificacion", notificacion);
                    }
                }

                envio.Guias.Add(guiaToBBDD);
            }

            envio.FechaUltimoMov = DateTime.Now;
            envio.UsuarioUltimoMovId = usuario.Id;

            if (envio.UsuarioId == 0)
                envio.UsuarioId = usuario.Id;

            envio.Transportista = null;
            envio.Chofer = null;
            envio.Vehiculo = null;
            envio.Estado = null;
            envio.EstadoId = (int)eEnviosEstados.Correcto;

            context.Envios.Update(envio);
            await context.SaveChangesAsync();

            notificacion = new NotificacionDTO
            {
                Mensaje = $" FIN DE ENVIO Nº {envio.Numero} <small>{DateTime.Now}</small> ",
                Usuario = usuario.Nombre,
                TipoMensaje = eTipoMensaje.Ok
            };

            await _notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);
        }
        catch (Exception ex)
        {
            var notificacion = new NotificacionDTO
            {
                Mensaje = $"<span class='text-red'> E R R O R  {ex.Message} <small> {DateTime.Now}</small>  </span>",
                Usuario = usuario.Nombre,
                TipoMensaje = eTipoMensaje.Error
            };

            await _notificationHubContext.Clients.Group("Notificacion")
                .SendAsync("ReceiveNotificacion", notificacion);

            return MessageDTO.Error(ex.Message);
        }

        return MessageDTO.Ok("Envío sincronizado con éxito.");
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



    private async Task<MessageDTO> EnviarALogictrackerAsync(Tracker_DevelContext context, Envio? envio, UsuarioDTO usuario)
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

            if (wsSetting.EntornoPruebas?.Activo ?? false)
            {
                url = wsSetting.EntornoPruebas.URL;
                prefijoTest = wsSetting.EntornoPruebas.Prefijo;
            }

            using var client = new CrearDistribucionConEntidadesSoapClient(
                CrearDistribucionConEntidadesSoapClient.EndpointConfiguration.CrearDistribucionConEntidadesSoap, url);

            // Logging SOAP si está habilitado
            var logCfg = _configuration.GetSection("SoapLogging").Get<SoapLoggingOptions>() ?? new();

            if (logCfg.Enabled && !client.Endpoint.EndpointBehaviors.OfType<SoapLoggingBehavior>().Any())
                client.Endpoint.EndpointBehaviors.Add(new SoapLoggingBehavior(_logger, logCfg.ToFile, logCfg.Path));

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

            var notificacion = new NotificacionDTO();

            envio!.Estado = null;
            envio.EstadoId = (int)eEnviosEstados.Correcto;

            // ---------------- PROCESAR GUIAS ----------------

            foreach (var guia in listGuias)
            {
                var listClientes = new List<ClienteWs>();

                var articulos = articulosPorGuia.TryGetValue(guia.Numero, out var arts)
                    ? arts
                    : new List<ArticuloDTO>();

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
                
                // Descripcion: {prefijo}{ClienteNombre} – {ClienteAfiliado} (si hay afiliado)
                var descripcionCliente = string.Concat(
                    prefijoTest,
                    guia.ClienteNombre ?? string.Empty,
                    string.IsNullOrWhiteSpace(guia.ClienteAfiliado) ? string.Empty : string.Concat("–", guia.AfiliadoNombre)
                );

                // ---------------- TELEFONO ----------------

                var telefono = await ObtenerTelefonoClienteAsync(
                    articulos,
                    guia,
                    context,
                    usuario,
                    envio.Numero
                );

                listClientes.Add(new ClienteWs
                {
                    Codigo = guia.ClienteAfiliado,
                    Descripcion = descripcionCliente,
                    Coordenadas = envio.TransportistaDestino?.Coordenadas ?? guia.Coordenadas,
                    Direccion = envio.TransportistaDestino?.Direccion ?? guia.ClienteDireccion,
                    Telefono = telefono,
                    Remitos = listRemitos.ToArray()
                });

                request.Clientes = listClientes.ToArray();

                string guiaEstado = string.Empty;

                var guiaToBBDD = new EnvioGuia
                {
                    Fecha = guia.Fecha,
                    Numero = guia.Numero
                };

                GenericResponse resp;

                if (!wsSetting.Activo) // bypass WS
                {
                    resp = new GenericResponse { Codigo = 200 };
                }
                else
                {
                    // Logging por guía si aplica
                    if (logCfg.Enabled && logCfg.ToFile)
                    {
                        using (SoapLogContext.UseGuia(guia.Numero.ToString()))
                        {
                            var result = await client.CreateDistribucionConEntidadesAsync(request);
                            resp = result.Body.CreateDistribucionConEntidadesResult;
                        }
                    }
                    else
                    {
                        var result = await client.CreateDistribucionConEntidadesAsync(request);
                        resp = result.Body.CreateDistribucionConEntidadesResult;
                    }
                }

                // ---------------- RESPUESTA WS ----------------

                if (resp.Codigo == 200)
                {
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.Correcto;

                    var envioAudit = new EnvioAudit
                    {
                        Envio = envio.Numero,
                        EstadoId = (int)eEnviosEstados.Correcto,
                        Fecha = DateTime.Now,
                        Guia = guia.Numero,
                        Usuario = usuario.Nombre,
                        Direccion = envio.TransportistaDestino?.Direccion,
                        CodigoViaje = envio.CodigoViaje
                    };

                    await _envioAuditService.AuditarEnvioAsync(context, envioAudit);
                }
                else
                {
                    guiaToBBDD.EstadoId = (int)eEnviosEstados.ConError;
                    envio.EstadoId = (int)eEnviosEstados.ConError;

                    Error.WriteLog($"ERROR {resp.Codigo} - {resp.Mensaje} Envio: {envio.Numero} Guia: {guia.Numero}");
                }

                envio.Guias.Add(guiaToBBDD);
            }

            envio.FechaUltimoMov = DateTime.Now;
            envio.UsuarioUltimoMovId = usuario.Id;

            if (envio.UsuarioId == 0)
                envio.UsuarioId = usuario.Id;

            envio.Transportista = null;
            envio.Chofer = null;
            envio.Vehiculo = null;
            envio.Estado = null;

            context.Envios.Update(envio);
            await context.SaveChangesAsync();

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

    private async Task<string?> ObtenerTelefonoClienteAsync(
        IEnumerable<ArticuloDTO> articulos,
        GuiaDTO guia,
        Tracker_DevelContext context,
        UsuarioDTO usuario,
        int? envioNumero = null)
    {
        var telefonos = articulos
            .Where(a => !string.IsNullOrWhiteSpace(a.Telefono) && a.Telefono != "ERROR")
            .Select(a => new
            {
                Afiliado = a.CabeceraComprobantesAfiliado,
                Telefono = a.Telefono?.Trim(),
                a.ListaPrecio
            })
            .Distinct()
            .ToList();

        if (telefonos.Count == 1)
            return telefonos[0].Telefono;

        if (telefonos.Count > 1)
        {
            Error.WriteLog($"ERROR TELEFONO MULTIPLE - Envio: {envioNumero} Guia: {guia.Numero}");

            foreach (var tel in telefonos)
            {
                await context.TelefonosGuiasLog.AddAsync(new TelefonoGuiaLog
                {
                    NumGuia = guia.Numero,
                    Cliente = guia.ClienteCodigo,
                    Afiliado = tel.Afiliado,
                    Listapre = tel.ListaPrecio,
                    FechaRegistro = DateTime.Now,
                    TelefonoEstado = "MULTIPLES_AFILIADOS_TELEFONO",
                    UsuarioRegistra = usuario.Nombre
                });
            }

            await context.SaveChangesAsync();
        }

        return null;
    }
}
