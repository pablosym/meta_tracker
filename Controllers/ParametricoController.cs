using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq.Expressions;
using Tracker.DTOs;
using Tracker.Models;

namespace Tracker.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public class ParametricoController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    private void Populate(ParametricoDTO? parametricoDTO)
    {
        var listaHeaders = _context.ParametricosHeader
            .OrderBy(x => x.Descripcion)
            .ToList();

        ViewData["IdTablaLookup"] = parametricoDTO != null
            ? new SelectList(listaHeaders, "Id", "Descripcion", parametricoDTO.Id)
            : new SelectList(listaHeaders, "Id", "Descripcion");
    }


    [IgnoreAntiforgeryToken]
    public IActionResult Index()
    {
        Populate(null);
        return View();
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lookup = await _context.ParametricosHeader
            .Include(l => l.Parametricos)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (lookup == null)
        {
            return NotFound();
        }

        return View(lookup);
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Create()
    {
        Populate(null);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ParametricoDTO parametricoDTO)
    {
        if (ModelState.IsValid)
        {

            var lookup = Helpers.Mappers.MapTo(parametricoDTO);

            _context.Add(lookup);

            await _context.SaveChangesAsync();

            SetMessage(new MessageDTO() { Status = MessageDTO.Estatus.OK, Value = "Los datos grabados con éxito" });
            ModelState.Clear();
            //return RedirectToAction(nameof(Index));
        }

        Populate(null);
        return View();
    }


    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var parametrico = await _context.Parametricos.FindAsync(id);
        if (parametrico == null)
        {
            return NotFound();
        }


        ParametricoDTO parametricoDTO = Helpers.Mappers.MapTo(parametrico);
        Populate(parametricoDTO);
        return View(parametricoDTO);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ParametricoDTO parametricoDTO)
    {
        if (id != parametricoDTO.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            Parametrico lookup = Helpers.Mappers.MapTo(parametricoDTO);

            _context.Update(lookup);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        Populate(parametricoDTO);
        return View(parametricoDTO);
    }

    [HttpPost]
    public async Task<IActionResult> GetIndex()
    {
        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var param = Request.Form["param"].FirstOrDefault();
        var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
        var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        int skip = start != null ? Convert.ToInt32(start) : 0;
        int recordsTotal = 0;


        List<FiltroListasParamDTO>? listParam = null;

        if (!string.IsNullOrEmpty(param))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);



        IQueryable<Parametrico> sqlData = _context.Parametricos;

        if (listParam != null)
        {
            var parametricosHeaderId = listParam.FirstOrDefault(s => s.Key == "parametricosHeaderId" && s.Value != "")?.Value ?? "";
            var descripcion = listParam.FirstOrDefault(s => s.Key == "descripcion" && s.Value != "")?.Value ?? "";


            if (!string.IsNullOrEmpty(parametricosHeaderId))
            {
                sqlData = sqlData.Where(w => w.ParametricosHeaderId == int.Parse(parametricosHeaderId));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                sqlData = sqlData.Where(w => w.Descripcion.StartsWith(descripcion));
            }
        }

        //
        // Ordenamiento
        //
        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {

            Expression<Func<Parametrico, object>> orderBy = o => o.Descripcion;

            switch (sortColumn)
            {
                case "codigo":
                    orderBy = o => o.Codigo;
                    break;
                case "descripcion":
                    orderBy = o => o.Descripcion;
                    break;
                case "TablaLookup":
                    orderBy = o => o.ParametricosHeader.Descripcion;
                    break;

            }
            if (sortColumnDirection == "asc")
                sqlData = sqlData.OrderBy(orderBy); // sortColumn + " " + sortColumnDirection);
            else
                sqlData = sqlData.OrderByDescending(orderBy); // sortColumn + " " + sortColumnDirection);
        }

        recordsTotal = sqlData.Count();

        var data = await sqlData.Skip(skip).Take(pageSize).Select(s => new ParametricoDTO()
        {
            Codigo = s.Codigo,
            Baja = s.Baja,
            Color = s.Color,
            Descripcion = s.Descripcion,
            Orden = s.Orden,
            Valor = s.Valor,
            Id = s.Id,
            ParametricosHeaderId = s.ParametricosHeaderId

        }).ToListAsync();

        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);

    }

    public JsonResult AutoComplete(string term)
    {

        try
        {

            var routeList = _context.Parametricos.Where(w => w.Descripcion.StartsWith(term))
            .Take(30)
            .Select(r => new { id = r.Id, label = r.Descripcion, name = "Id" });
            return Json(routeList);
        }

        catch
        {
            return Json(MessageDTO.Error("No se encontraron datos."));
        }
    }
}
