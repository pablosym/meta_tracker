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
public class TransportistasController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    public JsonResult AutoComplete(string term)
    {

        try
        {

            var routeList = _context.vwTransportistas
                .Where(w => w.Activo == true && w.Nombre.StartsWith(term))
            .Take(15)
            .Select(r => new AutoCompleteDTO()
            {
                Id = r.Idprovlogi.ToString(),
                Label = r.Nombre,
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

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index()
    {
        await Populate(null);
        return View();
    }

    private async Task Populate(TransportistaDTO? TransportistaDTO)
    {

        var listEstados = await _context.Parametricos.Where(w => w.Baja == false
                                                                                && w.ParametricosHeaderId == (int)Constants.eParametricosHeader.TransportistaEstado)
                                                                            .OrderBy(o => o.Orden).ToListAsync();
        if (TransportistaDTO == null)
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion");
        }
        else
        {
            ViewData["listEstados"] = new SelectList(listEstados, "Id", "Descripcion", TransportistaDTO.EstadoId);
        }

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
        
        if  (!string.IsNullOrEmpty( param ))
            listParam = JsonConvert.DeserializeObject<List<FiltroListasParamDTO>>(param);



        IQueryable<vwTransportista> sqlData = _context.vwTransportistas;
                                    
        if (listParam == null || listParam.Count == 0)
        {
            listParam = new List<FiltroListasParamDTO>();
        }   

        var codigo = listParam.FirstOrDefault(s => s.Key == "codigo" && s.Value != "")?.Value ?? "";
        var estadoId = listParam.FirstOrDefault(s => s.Key == "estadoId" && s.Value != "")?.Value ?? "0";
        var nombre = listParam.FirstOrDefault(s => s.Key == "nombre" && s.Value != "")?.Value ?? "";
        

        if (!string.IsNullOrEmpty(codigo))
        {
            sqlData = sqlData.Where(w => w.Codigo.Equals(decimal.Parse(codigo)));
        }

        if (!string.IsNullOrEmpty(nombre))
        {
            sqlData = sqlData.Where(w => w.Nombre.StartsWith((nombre)));
        }
        if (!string.IsNullOrEmpty(estadoId) && estadoId != "-1")
        {
            sqlData = sqlData.Where(w => w.EstadoId.Equals(int.Parse(estadoId)));
        }

        //
        // Ordenamiento
        //
        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {

            Expression<Func<vwTransportista, object>> orderBy = o => o.Codigo;

            switch (sortColumn)
            {
                case "codigo":
                    orderBy = o => o.Codigo;
                    break;
                case "nombre":
                    orderBy = o => o.Nombre;
                    break;
                case "direccion":
                    orderBy = o =>  o.Direccion ??"";
                    break;
                case "estadoId":
                    orderBy = o => (object)o.Estado!.Descripcion!;
                    break;
            }
            if (sortColumnDirection == "asc")
                sqlData = sqlData.OrderBy(orderBy);
            else
                sqlData = sqlData.OrderByDescending(orderBy);
        }

        recordsTotal = sqlData.Count();

        var data = await sqlData.Skip(skip).Take(pageSize).Select(s => new TransportistaDTO()
        {
            Idprovlogi =   s.Idprovlogi, 
            Codigo =  s.Codigo, 
            Nombre = s.Nombre,
            Direccion = s.Direccion, 
            EstadoId = s.EstadoId ?? -1,
            Estado =  (s.Estado == null ? "" : s.Estado.Descripcion),
            EstadoColor = (s.Estado == null ? "" : s.Estado.Color)  
        }).ToListAsync();

        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Edit(decimal? Id)
    {
        if (Id == null || _context.vwTransportistas == null)
        {
            return NotFound();
        }

        var transportista = await _context.vwTransportistas.Include(i => i.Estado).FirstOrDefaultAsync(x => x.Codigo == Id);

        if (transportista == null)
        {
            return NotFound();
        }

        var transportistaDTO = Mappers.MapTo(transportista);
        await Populate(null);

        return View(transportistaDTO);
    }

    [HttpPost]

    public async Task<IActionResult> Edit(decimal id, [Bind("Codigo, Nombre, Direccion, EstadoId, Coordenadas")] TransportistaDTO transportistaDTO)
    {
        if (id != transportistaDTO.Codigo)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            //busco la entidad en la BBDD 
            var vwTtransportista = await _context.vwTransportistas.FirstOrDefaultAsync(x => x.Codigo.Equals(transportistaDTO.Codigo));


            if (vwTtransportista == null)
            {
                await Populate(null);
                SetMessage(new MessageDTO() { Status = MessageDTO.Estatus.ERROR, Value = "El Transportista no existe" });
                return View(transportistaDTO);
            }


            var transportista = await _context.Transportistas.FirstOrDefaultAsync(x => x.Codigo.Equals(transportistaDTO.Codigo));

            transportista ??= new Transportista();

            transportista.Codigo = vwTtransportista.Codigo;
            transportista.Nombre = vwTtransportista.Nombre;
            transportista.Direccion = transportistaDTO.Direccion ?? "";
            transportista.EstadoId = transportistaDTO.EstadoId;
            transportista.Coordenadas = transportistaDTO.Coordenadas;


            _context.Transportistas.Attach(transportista);


            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await Populate(null);
        return View(transportistaDTO);
    }

}
