using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Tracker.DTOs;
using Tracker.Models;

namespace Tracker.Controllers;

[Authorize]
public class AuditoriaTelefonosController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    public IActionResult Index()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> GetIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
        var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");

        var sortColumn = Request.Form[$"columns[{Request.Form["order[0][column]"]}][name]"].FirstOrDefault() ?? "";
        var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();

        var paramJson = Request.Form["param"].FirstOrDefault();
        var filtro = JsonConvert.DeserializeObject<FiltroAuditoriaDTO>(paramJson ?? "{}");

        var query = _context.TelefonosGuiasLog.AsNoTracking();

        // filtros
        if (!string.IsNullOrWhiteSpace(filtro?.Usuario))
            query = query.Where(x => x.UsuarioRegistra == filtro.Usuario);

        if (!string.IsNullOrWhiteSpace(filtro?.Estado))
            query = query.Where(x => x.TelefonoEstado == filtro.Estado);

        if (DateTime.TryParse(filtro?.Desde, out var fechaDesde))
        {
            query = query.Where(x => x.FechaRegistro.Date >= fechaDesde.Date);
        }

        if (DateTime.TryParse(filtro?.Hasta, out var fechaHasta))
        {
            query = query.Where(x => x.FechaRegistro.Date <= fechaHasta.Date);
        }


        var searchValue = Request.Form["search[value]"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(x =>
                x.NumGuia.ToString().Contains(searchValue) ||
                x.Cliente.ToString().Contains(searchValue) ||
                x.Afiliado.ToString().Contains(searchValue) ||
                x.Listapre.Contains(searchValue) ||
                x.TelefonoEstado.Contains(searchValue) ||
                (x.NombreAfiliado != null && x.NombreAfiliado.Contains(searchValue)) ||
                (x.UsuarioRegistra != null && x.UsuarioRegistra.Contains(searchValue))
            );
        }

        var recordsTotal = await query.CountAsync();

        var ascending = sortDirection?.ToLower() == "asc";
        query = OrdenarTelefonosGuiasLog(query, sortColumn, ascending);

        var data = await query
            .Skip(start)
            .Take(length)
            .Select(x => new TelefonoGuiaLogDTO
            {
                NumGuia = x.NumGuia,
                Cliente = x.Cliente,
                Afiliado = x.Afiliado,
                Listapre = x.Listapre,
                UsuarioRegistra = x.UsuarioRegistra,
                FechaRegistro = x.FechaRegistro,
                NombreAfiliado = x.NombreAfiliado
            })
            .ToListAsync();

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data
        });
    }

    private static IQueryable<TelefonoGuiaLog> OrdenarTelefonosGuiasLog(IQueryable<TelefonoGuiaLog> query, string sortColumn, bool ascending)
    {
        return sortColumn?.ToLowerInvariant() switch
        {
            "numguia" => ascending ? query.OrderBy(x => x.NumGuia) : query.OrderByDescending(x => x.NumGuia),
            "cliente" => ascending ? query.OrderBy(x => x.Cliente) : query.OrderByDescending(x => x.Cliente),
            "afiliado" => ascending ? query.OrderBy(x => x.Afiliado) : query.OrderByDescending(x => x.Afiliado),
            "listapre" => ascending ? query.OrderBy(x => x.Listapre) : query.OrderByDescending(x => x.Listapre),
            "afiliadonombre" => ascending ? query.OrderBy(x => x.NombreAfiliado) : query.OrderByDescending(x => x.NombreAfiliado),
            "usuarioregistra" => ascending ? query.OrderBy(x => x.UsuarioRegistra) : query.OrderByDescending(x => x.UsuarioRegistra),
            "fecharegistro" => ascending ? query.OrderBy(x => x.FechaRegistro) : query.OrderByDescending(x => x.FechaRegistro),
            _ => query.OrderByDescending(x => x.FechaRegistro)
        };
    }
}