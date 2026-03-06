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
public class VehiculosController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{


    // GET: Vehiculos

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index()
    {
        await Populate(null);
        return View();
    }
    private async Task Populate(VehiculoDTO? vehiculoDTO)
    {
        //Combo tipo de vehiculos
        var listVehiculos = await _context.Parametricos.Where(w => w.Baja == false                                                                                   
                                                                            && w.ParametricosHeaderId == (int)Constants.eParametricosHeader.TipoVehiculo)
                                                                            .OrderBy(o => o.Orden).ToListAsync();
        if (vehiculoDTO == null){
            ViewData["listVehiculos"] = new SelectList(listVehiculos, "Id", "Descripcion");
        }else{
            ViewData["listVehiculos"] = new SelectList(listVehiculos, "Id", "Descripcion", vehiculoDTO.TipoId);
        }

        //Combo estados
        var listEstados = await _context.Parametricos.Where(w => w.Baja == false
                                                                                && w.ParametricosHeaderId == (int)Constants.eParametricosHeader.VehiculoEstado)
                                                                            .OrderBy(o => o.Orden).ToListAsync();
        if (vehiculoDTO == null)
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion");
        }
        else
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion", vehiculoDTO.EstadoId);
        }
    }


    // GET: Vehiculos/Create
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create()
    {
        await Populate(null);
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Create([Bind("Id,Descripcion,Patente,TipoId")] VehiculoDTO vehiculoDTO, string? returnUrl)
    {
        //
        // Hay que encontrar la forma de hacerlo generico a esto, con el enum va directo al ID de la tabla, 
        // esta mal, pero no tan mal.
        //

        //var parametricoActivo = 
        //   _context.Parametricos.SingleOrDefault(x => x.Descripcion == "Activo" && x.Codigo == "Vehiculo Estado"); 

        //if (parametricoActivo != null){
        //    vehiculoDTO.EstadoId = parametricoActivo.Id; //Pone Estado Activo por default
        //}

        if (ModelState.IsValid)
        {

            var existePantente = _context.Vehiculos.Any(a => a.Patente == vehiculoDTO.Patente);

            if (existePantente)
            {
                return Ok(MessageDTO.Warning($"ATENCION La Pantente ingresada: {vehiculoDTO.Patente} ya existe."));
            }

            var vehiculo = Mappers.MapTo(vehiculoDTO);
            vehiculo.EstadoId = (int)Tracker.Helpers.Constants.eVehiculosEstados.Activo;

            _context.Add(vehiculo);

            await _context.SaveChangesAsync();


            if (!string.IsNullOrWhiteSpace(returnUrl))
            {

                if (returnUrl == "js")
                {
                    var vehiculoFromBBDD = await _context.Vehiculos.Include( i=>i.Tipo).Include(i=>i.Estado).FirstAsync(x=>x.Id == vehiculo.Id);
                    return Ok(MessageDTO.Ok("Registro grabado con éxito"));
                }
            }

            return Ok(MessageDTO.Redirect("/Vehiculos/Index"));
        }

        await Populate(null);
        return View(vehiculoDTO);
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
        if (id == null || _context.Vehiculos == null)
        {
            return NotFound();
        }

        var vehiculo = await _context.Vehiculos.Include(i => i.Tipo).FirstOrDefaultAsync(x => x.Id == id);

        if (vehiculo == null)
        {
            return NotFound();
        }

        var vehiculoDTO = Mappers.MapTo(vehiculo);
        await Populate(null);

        return View(vehiculoDTO);
    }

    [HttpPost]

    public async Task<IActionResult> Edit(int id, [Bind("Id,Descripcion,Patente,TipoId,EstadoId")] VehiculoDTO vehiculoDTO)
    {
        if (id != vehiculoDTO.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            //busco la entidad en la BBDD 
            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(x => x.Id.Equals(id));


            if (vehiculo == null)
            {
                await Populate(null);
                SetMessage(new MessageDTO() { Status = MessageDTO.Estatus.ERROR, Value = "El vehiculo no existe" });
                return View(vehiculoDTO);
            }


            vehiculo = Mappers.MapTo(vehiculo, vehiculoDTO);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await Populate(null);
        return View(vehiculoDTO);
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


        IQueryable<Vehiculo> sqlData = _context.Vehiculos
                                    .Include(i => i.Tipo);

        if (listParam != null)
        {
            var patente = listParam.FirstOrDefault(s => s.Key == "patente" && s.Value != "")?.Value ?? "";
            var descripcion = listParam.FirstOrDefault(s => s.Key == "descripcion" && s.Value != "")?.Value ?? "";
            var tipoId = listParam.FirstOrDefault(s => s.Key == "tipoId" && s.Value != "")?.Value ?? "-1";
            var estadoId = listParam.FirstOrDefault(s => s.Key == "estadoId" && s.Value != "")?.Value ?? "-1";


            if (!string.IsNullOrEmpty(patente))
            {
                sqlData = sqlData.Where(w => w.Patente.StartsWith(patente));
            }

            if (!string.IsNullOrEmpty(descripcion))
            {
                sqlData = sqlData.Where(w => w.Descripcion.Equals(descripcion));
            }

            if (!string.IsNullOrEmpty(estadoId) && estadoId != "-1")
            {
                sqlData = sqlData.Where(w => w.EstadoId == int.Parse(estadoId));
            }

            if (!string.IsNullOrEmpty(tipoId) && tipoId != "-1")
            {
                sqlData = sqlData.Where(w => w.TipoId == int.Parse(tipoId));
            }
        }

        //
        // Ordenamiento
        //
        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {

            Expression<Func<Vehiculo, object>> orderBy = o => o.Patente;

            switch (sortColumn)
            {
                case "patente":
                    orderBy = o => o.Patente;
                    break;
                case "descripcion":
                    orderBy = o => o.Descripcion;
                    break;
                case "tipoId":
                    orderBy = o => (object?)o.Tipo!.Descripcion!;
                    break;
                case "estadoId":
                    orderBy = o => (object?)o.Estado!.Descripcion!;
                    break;


            }
            if (sortColumnDirection == "asc")
                sqlData = sqlData.OrderBy(orderBy);
            else
                sqlData = sqlData.OrderByDescending(orderBy);
        }

        recordsTotal = sqlData.Count();

        var data = await sqlData.Skip(skip).Take(pageSize).Select(s => new VehiculoDTO()
        {
            Patente = s.Patente,
            Descripcion = s.Descripcion,
            Id = s.Id,
            Tipo = s.Tipo == null ? "" : s.Tipo.Descripcion,
            Estado = s.Estado == null  ?"" :  s.Estado.Descripcion,
            EstadoColor = s.Estado == null ? "" : s.Estado.Descripcion

        }).ToListAsync();

        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);
    }


    public JsonResult AutoComplete(string term)
    {

        try
        {

            var routeList = _context.Vehiculos.Include(i =>i.Tipo)
                .Where(w => w.Patente.StartsWith(term))
            .Take(30)
            .Select(r => new AutoCompleteDTO()
            {
                Id = r.Id.ToString(),
                Label = r.Patente,
                Name = "Id",
                TagObj = JsonConvert.SerializeObject(r) 
            });

            return Json(routeList);
        }

        catch
        {
            return Json(MessageDTO.Error("No se encontraron datos."));
        }
    }
}
