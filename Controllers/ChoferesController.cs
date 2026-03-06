using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq.Expressions;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;


namespace Tracker.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public class ChoferesController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    private async Task Populate(ChoferDTO? ChoferDTO)
    {

        var listEstados = await _context.Parametricos.Where(w => w.Baja == false
                                                                                && w.ParametricosHeaderId == (int)Constants.eParametricosHeader.ChoferEstado)
                                                                            .OrderBy(o => o.Orden).ToListAsync();
        if (ChoferDTO == null)
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion");
        }
        else
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion", ChoferDTO.EstadoId);
        }

    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index()
    {
        await Populate(null);
        return View();
    }



    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create()
    {
        await Populate(null);
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Create([Bind("Id,ApellidoNombre,Legajo,Observacion,Telefono,Dni")] ChoferDTO choferDTO, string? returnUrl)
    {
        if (ModelState.IsValid)
        {
            var existeChofer = _context.Choferes.Any(a => a.Legajo == choferDTO.Legajo || a.Dni == choferDTO.Dni );

            if (existeChofer)
            {
                return Ok(MessageDTO.Warning($"ATENCION El chofer con legajo {choferDTO.Legajo}  DNI {choferDTO.Dni} ya existe."));
            }

            var chofer = Mappers.MapTo(choferDTO);

            chofer.EstadoId = (int)Constants.eChoferEstados.Activo;
            
            _context.Add(chofer);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {

                if (returnUrl == "js")
                {
                    return Ok(MessageDTO.Ok("Registro grabado con éxito"));
                }
            }

            return Ok(MessageDTO.Redirect("/Choferes/Index"));

        }

        await Populate(null);
        return View(choferDTO);
    }


    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreatePartial()
    {
        await Populate(null);
        return PartialView();
    }


    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null || _context.Choferes == null)
        {
            return NotFound();
        }

        var chofer = await _context.Choferes.Include(i =>i.Estado).FirstOrDefaultAsync(x => x.Id == id);

        if (chofer == null)
        {
            return NotFound();
        }

        var choferDTO = Mappers.MapTo(chofer);
        await Populate(null);

        return View(choferDTO);
    }

    [HttpPost]

    public async Task<IActionResult> Edit(int id, [Bind("Id,ApellidoNombre,Legajo,Observacion,Telefono,Dni,EstadoId")] ChoferDTO choferDTO)
    {
        if (id != choferDTO.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            //busco la entidad en la BBDD 
            var chofer = await _context.Choferes.FirstOrDefaultAsync(x => x.Id.Equals(id));


            if (chofer == null)
            {
                await Populate(null);
                SetMessage(new MessageDTO() { Status = MessageDTO.Estatus.ERROR, Value = "El chofer no existe" });
                return View(choferDTO);
            }


            chofer = Mappers.MapTo(chofer, choferDTO);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await Populate(null);
        return View(choferDTO);
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

        
        IQueryable<Chofer> sqlData = _context.Choferes
                                    .Include(i => i.Estado);

        if (listParam != null)
        {
            var razonSocial = listParam.FirstOrDefault(s => s.Key == "razonSocial" && s.Value != "")?.Value ?? "";
            var estadoId = listParam.FirstOrDefault(s => s.Key == "estadoId" && s.Value != "")?.Value ?? "0";
            var dni = listParam.FirstOrDefault(s => s.Key == "dni" && s.Value != "")?.Value ?? "";


            if (!string.IsNullOrEmpty(razonSocial))
            {
                sqlData = sqlData.Where(w => w.ApellidoNombre.StartsWith(razonSocial));
            }

            if (!string.IsNullOrEmpty(dni))
            {
                sqlData = sqlData.Where(w => w.Dni.Equals(dni));
            }
            if (!string.IsNullOrEmpty(estadoId) && estadoId != "-1")
            {
                sqlData = sqlData.Where(w => w.EstadoId == int.Parse(estadoId));
            }
        }
        else if (!string.IsNullOrEmpty(searchValue))
        {
            sqlData = sqlData.Where(w => w.ApellidoNombre.Contains(searchValue) || w.Legajo.Contains(searchValue) || w.Dni.Contains(searchValue));
        }   

        //
        // Ordenamiento
        //
        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {

            Expression<Func<Chofer, object>> orderBy = o => o.ApellidoNombre;

            switch (sortColumn)
            {
                case "razonSocial":
                    orderBy = o => o.ApellidoNombre;
                    break;
                case "legajo":
                    orderBy = o => o.Legajo;
                    break;
                case "dni":
                    orderBy = o => o.Dni;
                    break;
                case "estadoId":
                    orderBy = o => o.Estado.Descripcion;
                    break;

            }
            if (sortColumnDirection == "asc")
                sqlData = sqlData.OrderBy(orderBy);
            else
                sqlData = sqlData.OrderByDescending(orderBy);
        }

        recordsTotal = sqlData.Count();

        var data = await sqlData.Skip(skip).Take(pageSize).Select(s => new ChoferDTO()
        {
            ApellidoNombre = s.ApellidoNombre,
            Observacion = s.Observacion,
            Dni = s.Dni,
            Id  = s.Id,
            Legajo = s.Legajo,
            Telefono = s.Telefono,
            Estado = s.Estado.Descripcion,
            EstadoColor = s.Estado.Color
        }).ToListAsync();

        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);
    }
    
    public JsonResult AutoComplete(string term)
    {

        try
        {

            var routeList = _context.Choferes
                .Where(w => w.Baja == false && w.ApellidoNombre.StartsWith(term))
            .Take(30)
            .Select(r => new AutoCompleteDTO()
            {
                Id = r.Id.ToString(),
                Label = r.ApellidoNombre,
                Name = "Id",
                TagObj = JsonConvert.SerializeObject(r)
            });

            return Json(routeList);
        }

        catch
        {
            return Json(MessageDTO.Error("No se encontraron Datos."));
        }
    }

    public JsonResult AutoCompleteByDNI(string term)
    {

        try
        {

            var routeList = _context.Choferes
                .Where(w => w.Baja == false && w.Dni.StartsWith(term))
            .Take(30)
            .Select(r => new AutoCompleteDTO()
            {
                Id = r.Id.ToString(),
                Label = r.Dni,
                Name = "Id",
                TagObj = JsonConvert.SerializeObject(r)
            });

            return Json(routeList);
        }

        catch
        {
            return Json(MessageDTO.Error("No se encontraron Datos."));
        }
    }

}
