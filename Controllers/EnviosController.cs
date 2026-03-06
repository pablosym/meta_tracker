using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public class EnviosController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor, IEnvioService envioService) : BaseController(context, contextAccessor)
{

    private readonly IEnvioService _envioService = envioService;

    private async Task Populate(EnvioDTO? envioDTO)
    {

        var listEstados = await _context.Parametricos.Where(w => w.Baja == false
                                                                                && w.ParametricosHeaderId == (int)Constants.eParametricosHeader.EnvioEstado)
                                                                            .OrderBy(o => o.Orden).ToListAsync();
        if (envioDTO == null)
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion");
        }
        else
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion", envioDTO.EstadoId);
        }

    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index()
    {
        await Populate(null);
        return View();
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create(int id)
    {

        var envioDTO = await _envioService.ObtenerPorIdAsync(id);
        return View(envioDTO);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EnvioDTO envioDTO)
    {
        if (!ModelState.IsValid)
            return Ok(MessageDTO.Error("El formulario contiene errores."));

        var result = await _envioService.GuardarAsync(envioDTO, this.UsuarioId);
        return Ok(result);

    }


    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PreView(int id)
    {
        var envioDTO = await _envioService.ObtenerPorIdAsync(id);
        return View(envioDTO);
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Sincronizar(int EnvioId)
    {

        var result = await _envioService.PrepararEnvioASincronizarAsync(EnvioId);

        if (!result.IsOk)
            return Ok(result);

        if (result.TagObj is not Envio envio)
            return Ok(MessageDTO.Error("No se pudo obtener el envio."));

        result = await Sincronizar(envio);

        if (!result.IsOk)
            return Ok(result);

        return RedirectToAction(nameof(Index));

    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SincronizarConGuia(int EnvioId, Int64 GuiaNumero)
    {

        var result = await _envioService.PrepararEnvioASincronizarAsync(EnvioId);

        if (!result.IsOk)
            return Ok(result);

        if (result.TagObj is not Envio envio)
            return Ok(MessageDTO.Error("No se pudo obtener el envio."));


        result = await Sincronizar(envio);

        if (!result.IsOk)
            return Ok(result);

        return RedirectToAction(nameof(Index));
    }

    private async Task<MessageDTO> Sincronizar(Envio envio)
    {
        var usuario = new UsuarioDTO { Id = this.UsuarioId, Nombre = this.UsuarioNombre };
        var result = await _envioService.SincronizarAsync(envio, null, usuario);
        return result;
    }

    private async Task<MessageDTO> Sincronizar(List<EnvioDTO> listEnvios)
    {
        if (listEnvios.Count == 0)
            return MessageDTO.Warning("Debe seleccionar algun envio para poder sincronizar");

        var usuario = new UsuarioDTO { Id = this.UsuarioId, Nombre = this.UsuarioNombre };
        var result = await _envioService.SincronizarAsync(null, listEnvios, usuario);
        return result;

    }

    [HttpPost]
    public async Task<IActionResult> GetIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var param = Request.Form["param"].FirstOrDefault();
        var sortColumn = Request.Form[$"columns[{Request.Form["order[0][column]"].FirstOrDefault()}][name]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var searchValue = Request.Form["search[value]"].FirstOrDefault();

        int pageSize = length != null ? Convert.ToInt32(length) : 10;
        int skip = start != null ? Convert.ToInt32(start) : 0;

        List<FiltroListasParamDTO>? listParam = null;

        if (!string.IsNullOrEmpty(param))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);

        var filtro = new FiltroEnvioDTO
        {
            PageSize = pageSize,
            Skip = skip,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            SearchValue = searchValue
        };

        if (listParam != null)
        {
            string? desde = listParam.FirstOrDefault(s => s.Key == "desde" && !string.IsNullOrWhiteSpace(s.Value))?.Value;
            string? hasta = listParam.FirstOrDefault(s => s.Key == "hasta" && !string.IsNullOrWhiteSpace(s.Value))?.Value;

            filtro.Numero = int.TryParse(listParam.FirstOrDefault(s => s.Key == "numero")?.Value, out var num) ? num : 0;
            filtro.GuiaNumero = int.TryParse(listParam.FirstOrDefault(s => s.Key == "guia")?.Value, out var guia) ? guia : 0;
            filtro.EstadoId = int.TryParse(listParam.FirstOrDefault(s => s.Key == "estadoId")?.Value, out var est) ? est : 0;

            // Si no busca por número ni guía, aplicar fechas (ya sea ingresadas o por default)
            if (filtro.Numero == 0 && filtro.GuiaNumero == 0)
            {
                filtro.Desde = desde ?? DateTime.Now.AddDays(-15).ToString("yyyy-MM-dd");
                filtro.Hasta = hasta ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            }
            else
            {
                // Si busca por número o guía, no debe filtrar por fecha
                filtro.Desde = null;
                filtro.Hasta = null;
            }
        }


        var data = await _envioService.ObtenerEnviosAsync(filtro);
        var recordsTotal = data.FirstOrDefault()?.RecordsTotal ?? 0;

        var jsonData = new
        {
            draw,
            recordsFiltered = recordsTotal,
            recordsTotal,
            data
        };

        return Ok(jsonData);
    }


    [HttpPost]
    public async Task<IActionResult> GetEnvio(FiltroEnvioDTO filtroDTO)
    {
        var envio = await _envioService.ObtenerEnvioConDatosAsync(filtroDTO);
        return Ok(envio);
    }


    [HttpPost]
    public async Task<IActionResult> GetGuiasIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var param = Request.Form["param"].FirstOrDefault();

        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        int skip = start != null ? Convert.ToInt32(start) : 0;

        var filtro = new FiltroEnvioDTO
        {
            PageSize = pageSize,
            Skip = skip
        };

        List<FiltroListasParamDTO>? listParam = null;

        if (!string.IsNullOrEmpty(param))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);


        if (listParam != null)
        {
            var numero = listParam.FirstOrDefault(s => s.Key == "numero" && s.Value != "")?.Value ?? "0";
            filtro.Numero = int.TryParse(numero, out var n) ? n : 0;
        }

        var data = await _envioService.ObtenerGuiasAsync(filtro, UsuarioNombre);

        var recordsTotal = data.FirstOrDefault()?.RecordsTotal ?? 0;

        var jsonData = new
        {
            draw,
            recordsFiltered = recordsTotal,
            recordsTotal,
            data
        };

        return Ok(jsonData);
    }


    [HttpPost]
    public async Task<IActionResult> GetArticulosIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var param = Request.Form["param"].FirstOrDefault();

        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        int skip = start != null ? Convert.ToInt32(start) : 0;

        var filtro = new FiltroEnvioDTO
        {
            PageSize = pageSize,
            Skip = skip
        };

        List<FiltroListasParamDTO>? listParam = null;

        if (!string.IsNullOrEmpty(param))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);


        if (listParam != null)
        {
            var numero = listParam.FirstOrDefault(s => s.Key == "numero" && s.Value != "")?.Value ?? "0";
            filtro.Numero = int.TryParse(numero, out var n) ? n : 0;
        }

        var data = await _envioService.ObtenerArticulosPorGuiaAsync(filtro, "");

        var recordsTotal = data.FirstOrDefault()?.RecordsTotal ?? 0;

        var jsonData = new
        {
            draw,
            recordsFiltered = recordsTotal,
            recordsTotal,
            data
        };

        return Ok(jsonData);
    }


    [IgnoreAntiforgeryToken]
    public IActionResult Masivos()
    {
        var envioDTO = new EnvioDTO
        {
            FechaTurno = DateTime.Now,
            FechaInicio = DateTime.Now
        };

        return View(envioDTO);
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SendMasivos(EnvioDTO envioDTO)
    {

        try
        {

            var result = await Sincronizar(envioDTO.listEnvios);

            if (!result.IsOk)
                return Ok(result);

            return Ok(MessageDTO.Ok("Enviados Correctamente"));
        }
        catch (Exception ex)
        {
            Tracker.Helpers.Error.WriteLog(ex);
            return Ok(MessageDTO.Error("Problemas al sincronizar masivo"));
        }
    }
}