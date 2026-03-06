using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Tracker.DTOs;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.Controllers;

public class EnviosAuditController(Tracker_DevelContext context,
    IHttpContextAccessor contextAccessor,
    IEnvioAuditService envioAuditService,
    IEnvioService envioService) : BaseController(context, contextAccessor)
{
    private readonly IEnvioAuditService _envioAuditService = envioAuditService;
    private readonly IEnvioService _envioService = envioService;

    public async Task<IActionResult> Index()
    {
        var listEstados = await _envioService.ObtenerEstadosEnvioAsync();
        ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var param = Request.Form["param"].FirstOrDefault();

        var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();

        var searchValue = Request.Form["search[value]"].FirstOrDefault();

        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        int skip = start != null ? Convert.ToInt32(start) : 0;

        List<FiltroListasParamDTO>? listParam = null;

        if (!string.IsNullOrEmpty(param))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);

        var filtro = new FiltroAuditoriaDTO
        {
            PageSize = pageSize,
            Skip = skip,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            SearchValue = searchValue
        };

        if (listParam != null)
        {
            filtro.Desde = listParam.FirstOrDefault(s => s.Key == "desde" && !string.IsNullOrWhiteSpace(s.Value))?.Value;
            filtro.Hasta = listParam.FirstOrDefault(s => s.Key == "hasta" && !string.IsNullOrWhiteSpace(s.Value))?.Value;

            if (int.TryParse(listParam.FirstOrDefault(s => s.Key == "estadoId" && !string.IsNullOrWhiteSpace(s.Value))?.Value, out var estadoId))
                filtro.EstadoId = estadoId;

            if (Int64.TryParse(listParam.FirstOrDefault(s => s.Key == "envio" && !string.IsNullOrWhiteSpace(s.Value))?.Value, out var numero))
                filtro.Numero = numero;

            if (Int64.TryParse(listParam.FirstOrDefault(s => s.Key == "guia" && !string.IsNullOrWhiteSpace(s.Value))?.Value, out var guia))
                filtro.GuiaNumero = guia;

            filtro.Usuario = listParam.FirstOrDefault(s => s.Key == "usuario" && !string.IsNullOrWhiteSpace(s.Value))?.Value;

            if (filtro.Numero == null && filtro.GuiaNumero == null)
            {
                filtro.Desde ??= DateTime.Now.AddDays(-15).ToString("yyyy-MM-dd");
                filtro.Hasta ??= DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        var (data, recordsTotal) = await _envioAuditService.ObtenerAuditoriaEnvioAsync(filtro);

        return Ok(new
        {
            draw,
            recordsFiltered = recordsTotal,
            recordsTotal,
            data
        });
    }
}