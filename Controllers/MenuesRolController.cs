using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;

namespace Tracker.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public class MenuesRolController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    [IgnoreAntiforgeryToken]
    public IActionResult Index()
    {
        var menuesRolDTO = new MenuesRolDTO();
        Populate(null);
        return View(menuesRolDTO);
    }

    private void Populate(MenuesRolDTO? menuesRolDTO)
    {

        if (menuesRolDTO != null)
        {

            ViewData["MenuId"] = new MultiSelectList(_context.Menues
                              .Where(x => x.Baja == false)
                              .OrderBy(o => o.Id).ThenBy(o => o.Item)
                              .Select(x => new Menue { Id = x.Id, Item = "[ " + x.AspController + " ] " + x.Item }), "Id", "Item", menuesRolDTO.listIdMenu); 

        }

        ViewData["IdRol"] = new SelectList(_context.Parametricos.Where(w=> w.Baja == false && w.ParametricosHeaderId == (int) Constants.eParametricosHeader.Roles).OrderBy(o => o.Descripcion), "Id", "Descripcion");
    }


    [HttpPost]
    public async Task<IActionResult> Edit(MenuesRolDTO menuesRolDTO)
    {


        var listMenuSel = await _context.MenuesRoles.Where(x => x.RolId == menuesRolDTO.IdRol).Select(x => x.MenuId).ToListAsync();


        menuesRolDTO.listIdMenu = listMenuSel;

        Populate(menuesRolDTO);
        return View(menuesRolDTO);
    }
    [HttpPost]
    public async Task<IActionResult> Guardar(MenuesRolDTO menuesRolDTO)
    {

        if (menuesRolDTO.IdRol <= 0)
            return NotFound();

        var menuesRolBorrar = _context.MenuesRoles.Where(w => w.RolId == menuesRolDTO.IdRol);

        //borro todo lo que tenia
        _context.MenuesRoles.RemoveRange(menuesRolBorrar);


        foreach (var idMenu in menuesRolDTO.listIdMenu)
        {

            var menuesRol = new MenuesRole()
            {
                RolId = menuesRolDTO.IdRol,
                MenuId = idMenu,
            };

            _context.MenuesRoles.Add(menuesRol);

        }

        await _context.SaveChangesAsync();

        var messageDTO = new MessageDTO()
        {
            Status = MessageDTO.Estatus.OK,
            Value = "Datos grabados correctamente"
        };

        SetMessage(messageDTO);
        return RedirectToAction(nameof(Index));
    }
}
