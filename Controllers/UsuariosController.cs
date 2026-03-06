using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;

namespace Tracker.Controllers;


[Authorize]
[ValidateAntiForgeryToken]
public class UsuariosController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{
    [IgnoreAntiforgeryToken]
    public IActionResult Index()
    {
        return View();
    }

    
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(m => m.Id == id);
        if (usuario == null)
        {
            return NotFound();
        }

        return View(usuario);
    }

    private void Populate(UsuarioDTO? usuarioDTO)
    {



        if (usuarioDTO == null)
        {
            ViewData["IdRoles"] = new MultiSelectList(_context.Parametricos.Where(x => x.ParametricosHeaderId ==  (int)Constants.eParametricosHeader.Roles && x.Baja == false).OrderBy(o => o.Valor), "Id", "Descripcion");

        }

        else
        {
            var usuarioRoles = _context.UsuariosRoles
                                    .Where(w => w.UsuarioId == usuarioDTO.Id)
                                    .Select(x => x.RolId).ToList();

            ViewData["IdRoles"] = new MultiSelectList(_context.Parametricos.Where(x => x.ParametricosHeaderId == (int)Constants.eParametricosHeader.Roles && x.Baja == false).OrderBy(o => o.Valor), "Id", "Descripcion", usuarioRoles);
        }
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Create()
    {

        var usuarioDTO = new UsuarioDTO();
        Populate(null);
        return View(usuarioDTO);
    }

    [HttpPost]
    
    public async Task<IActionResult> Create([Bind("Baja, Password, ConfirmPassword,  ClienteCodigo, Correo, EmpresaId, Nombre, IdRolesSel, IdListaPreciosSel")] UsuarioDTO usuarioDTO)
    {
        if (ModelState.IsValid)
        {

            var usuario = new Usuario()
            {
                Baja = usuarioDTO.Baja,
                Clave = usuarioDTO.Password,
                Correo = usuarioDTO.Correo,
                FechaUltimoIngreso = DateTime.Now,
                FlgAdmin = false,
                Nombre = usuarioDTO.Nombre
            };


            string _KeyBase64 = Cryptography.PassKey();
            string _pass = usuario.Correo.ToLower() + usuario.Clave.Trim();

            usuario.Clave = Cryptography.EncryptText(_pass, _KeyBase64);

            _context.Add(usuario);
            await _context.SaveChangesAsync();
            //
            // Meto el rol
            //
            var usuariosRoles = new List<UsuariosRole>();

            for (int i = 0; i < usuarioDTO.IdRolesSel.Count; i++)
            {
                usuariosRoles.Add(new UsuariosRole()
                {
                    UsuarioId = usuario.Id,
                    RolId = usuarioDTO.IdRolesSel[i]
                });
            }

            _context.UsuariosRoles.AddRange(usuariosRoles);
            await _context.SaveChangesAsync();
            var messageDTO = new MessageDTO()
            {
                Status = MessageDTO.Estatus.OK,
                Value = "Datos grabados correctamente"
            };

            SetMessage(messageDTO);
            return RedirectToAction(nameof(Index));
        }

        Populate(usuarioDTO);
        return View(usuarioDTO);
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
                            .Include(i => i.UsuariosRoles)
                            .FirstOrDefaultAsync(x => x.Id == id);
        if (usuario == null)
        {
            Populate(null);
            return NotFound();
        }


        var usuarioDTO = new UsuarioDTO()
        {
            Baja = usuario.Baja,
            Password = usuario.Clave,
            ConfirmPassword =  usuario.Clave,
            Correo = usuario.Correo,
            Nombre = usuario.Nombre,
            FlgAdmin = usuario.FlgAdmin,
            IdRolesSel = usuario.UsuariosRoles.Select(s => s.RolId).ToList(),
        };

        Populate(usuarioDTO);

        return View(usuarioDTO);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id, Baja, Password, ConfirmPassword, ClienteCodigo, Correo, EmpresaId, Nombre, IdRolesSel,IdListaPreciosSel, FlgAdmin")] UsuarioDTO usuarioDTO)
    {
        if (id != usuarioDTO.Id)
        {
            Populate(usuarioDTO);
            return NotFound();
        }

        //if (ModelState.IsValid)
        //{

        var usuario = new Usuario()
        {
            Id = id,
            Baja = usuarioDTO.Baja,
            Clave = usuarioDTO.Password,
            Correo = usuarioDTO.Correo,
            FechaUltimoIngreso = DateTime.Now,
            FlgAdmin = usuarioDTO.FlgAdmin,
            Nombre = usuarioDTO.Nombre
        };

        _context.Update(usuario);
        //
        // Borro los roles que tenia
        //
        var usuarioRoles = _context.UsuariosRoles.Where(x => x.UsuarioId == usuario.Id);

        _context.UsuariosRoles.RemoveRange(usuarioRoles);
        //
        // Meto el rol
        //
        var usuariosRoles = new List<UsuariosRole>();

        for (int i = 0; i < usuarioDTO.IdRolesSel.Count; i++)
        {
            usuariosRoles.Add(new UsuariosRole()
            {
                UsuarioId = usuario.Id,
                RolId = usuarioDTO.IdRolesSel[i]
            });
        }

        _context.UsuariosRoles.AddRange(usuariosRoles);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
        
    }


    [HttpPost]
    public IActionResult GetIndex()
    {

        var draw = Request.Form["draw"].FirstOrDefault();
        var start = Request.Form["start"].FirstOrDefault();
        var length = Request.Form["length"].FirstOrDefault();
        var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
        var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
        var searchValue = Request.Form["search[value]"].FirstOrDefault();
        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        int skip = start != null ? Convert.ToInt32(start) : 0;
        int recordsTotal = 0;


        IQueryable<Usuario> sqlData = _context.Usuarios
                                .Include(i => i.UsuariosRoles)
                                .ThenInclude(i => i.Rol);

        

        if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
        {

            Expression<Func<Usuario, object>> orderBy = o => o.Nombre;

            switch (sortColumn)
            {
                case "nombre":
                    orderBy = o => o.Nombre;
                    break;
                case "correo":
                    orderBy = o => o.Correo;
                    break;

            }

            if (sortColumnDirection == "asc")
                sqlData = sqlData.OrderBy(orderBy); // sortColumn + " " + sortColumnDirection);
            else
                sqlData = sqlData.OrderByDescending(orderBy); // sortColumn + " " + sortColumnDirection);
        }
        

        recordsTotal = sqlData.Count();
        var data = sqlData.Skip(skip).Take(pageSize).ToList().Select(s => new
        {
            nombre = s.Nombre,
            correo = s.Correo,
            id = s.Id,
            baja = s.Baja
        });
        var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
        return Ok(jsonData);
    }
}

