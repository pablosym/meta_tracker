using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;

namespace Tracker.Controllers;

public class AccountController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDTO logingDTO)
    {
        try
        {

            if (logingDTO == null || logingDTO.Clave == null || logingDTO.Correo == null || !logingDTO.Correo.EsCorreoElectronicoValido())
            {
                return Ok(MessageDTO.Error("Usuario o contraseña no validos"));
            }

            string _KeyBase64 = Cryptography.PassKey();
            string _pass = logingDTO.Correo.ToLower() + logingDTO.Clave.Trim();

            logingDTO.Clave = Cryptography.EncryptText(_pass, _KeyBase64);

            var usuarioBBDD = await _context.Usuarios
                                        .Include(i => i.UsuariosRoles)
                                        .FirstOrDefaultAsync(x => x.Correo == logingDTO.Correo && x.Clave == logingDTO.Clave);

            if (usuarioBBDD != null)
            {
                //
                // Actualizo la ultima fecha de inicio de sesion.
                //
                usuarioBBDD.FechaUltimoIngreso = DateTime.Now;
                _context.Usuarios.Update(usuarioBBDD);
                await _context.SaveChangesAsync();


                var listMenues = new List<Menue>();
                //
                // Si el usuario esta marcado como administrador, le mando todo el menu.
                //
                if (usuarioBBDD.FlgAdmin)
                {

                    listMenues = await _context.Menues
                                .Where(x => x.Baja == false)
                                .OrderBy(o => o.Orden)
                                .ToListAsync();
                }
                else
                {
                    //
                    // Esto es todo el menu cargado
                    //
                    //var idGruposFuncionales = usuarioBBDD.GruposFuncionalesUsuarios.Select(x => x.IdGrupoFuncional).ToList();

                    //var listDataMenu = _context.GrupoFuncionalRoles
                    //        .Where(x => x.Baja == false && idGruposFuncionales.Contains(x.IdGrupoFuncional))
                    //        .Include(i => i.IdRolNavigation)
                    //        .ThenInclude(i => i.RolesModulos)
                    //        .ThenInclude(i => i.IdModuloNavigation)
                    //        .ThenInclude(i => i.ModulosMenues)
                    //        .ThenInclude(i => i.Menu)
                    //        .ThenInclude(i => i.MenuesAcciones)
                    //        .ToList();


                    var idRoles = usuarioBBDD.UsuariosRoles.Select(x => x.RolId).ToList();

                    var listDataMenu = await _context.MenuesRoles
                        .Include(i => i.Menu)
                        .OrderBy(o => o.Menu.Orden)
                        .Where(w => idRoles.Contains(w.RolId) && w.Menu.Baja == false)
                        .ToListAsync();

                    //
                    // opciones del menu segun los grupos funcionales.
                    //
                    foreach (var modulosMenue in listDataMenu)
                    {
                        var menue = new Menue()
                        {
                            AspAction = modulosMenue.Menu.AspAction,
                            AspController = modulosMenue.Menu.AspController,
                            Baja = modulosMenue.Menu.Baja,
                            Icono = modulosMenue.Menu.Icono,
                            Id = modulosMenue.Menu.Id,
                            MenuPadreId = modulosMenue.Menu.MenuPadreId,
                            Item = modulosMenue.Menu.Item,
                            Orden = modulosMenue.Menu.Orden,
                            AccesoDirecto = modulosMenue.Menu.AccesoDirecto

                        };

                        listMenues.Add(menue);
                    }

                    //
                    // Saco los items del menu repetidos 
                    //
                    listMenues = listMenues
                         .GroupBy(x => x?.Id)
                         .Select(g => g.FirstOrDefault())
                         .Where(m => m != null) 
                         .OrderBy(o => o!.Orden)
                         .ToList()!;

                }


                var listMenuDTO = new List<MenuDTO>();

                foreach (var item in listMenues)
                {
                    listMenuDTO.Add(new MenuDTO
                    {
                        AspAction = item.AspAction,
                        AspController = item.AspController,
                        Baja = item.Baja,
                        Icono = item.Icono,
                        IdMenu = item.Id,
                        IdMenuPadre = item.MenuPadreId,
                        Item = item.Item,
                        Orden = item.Orden,
                        AccesoDirecto = item.AccesoDirecto
                    });
                }

                var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme, ClaimTypes.Name, ClaimTypes.Role);

                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuarioBBDD.Id.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Name, usuarioBBDD.Nombre));
                identity.AddClaim(new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(listMenuDTO)));
                identity.AddClaim(new Claim(ClaimTypes.Email, usuarioBBDD.Correo));

                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.Now.AddHours(9)

                };
                //
                // borro la sesion 
                //
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

                var homeUrl = AppUrl("/Home/Index");

                MessageDTO messageDTO = new MessageDTO()
                {
                    Status = MessageDTO.Estatus.REDIRECT,
                    Value = homeUrl
                };

                return Ok(messageDTO);

            }
            else
            {
                ModelState.AddModelError("", "Usuario o contraseña no valido.");
                return Ok(MessageDTO.Error("Usuario o contraseña no validos"));
            }
        }
        catch (Exception ex)
        {
            Tracker.Helpers.Error.WriteLog(ex);
            return Ok(MessageDTO.Error("Error en conexion con el servidor."));
        }
    }
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Clave()
    {

        var claveDTO = new ClaveDTO();
        return View(claveDTO);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clave(ClaveDTO claveDTO)
    {
        if (!ModelState.IsValid)
        {
            return View(claveDTO);
        }


        var usuarioBBDD = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == Security.GetIdUsuario(User));

        if (usuarioBBDD != null)
        {
            //
            // Actualizo la ultima fecha de inicio de sesion.
            //
            //usuarioBBDD.FechaUltimaActualizacion = DateTime.Now;

            string _KeyBase64 = Cryptography.PassKey();

            string claveActualIngresada = usuarioBBDD.Correo.ToLower() + claveDTO.ClaveActual.Trim();
            string claveActualIngresadaEn = Cryptography.EncryptText(claveActualIngresada, _KeyBase64); //cambio de clave.

            if (claveActualIngresadaEn != usuarioBBDD.Clave)
            {
                var  messageDTO = new MessageDTO()
                {
                    Status = MessageDTO.Estatus.WARNING,
                    Value = "La contraseña ingresada no coincide con la actual. Verifique"
                };

                SetMessage(messageDTO);

                return RedirectToAction(nameof(AccountController.Clave), "Account");
            }

            string _pass = usuarioBBDD.Correo.ToLower() + claveDTO.ClaveNueva.Trim();
            usuarioBBDD.Clave = Cryptography.EncryptText(_pass, _KeyBase64); //cambio de clave.


            _context.Usuarios.Attach(usuarioBBDD);
            _context.Entry(usuarioBBDD).Property(x => x.Clave).IsModified = true;
            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        else
        {
            ModelState.AddModelError("", "Usuario o contraseña no valido.");

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }


    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaveReset(ClaveDTO claveDTO)
    {
        if (!ModelState.IsValid)
        {
            return View(claveDTO);
        }

        if (claveDTO.UsuarioId == null)
        {
            ModelState.AddModelError("", "Ningun Usuario Seleccionado");
            return View(claveDTO);
        }

        var usuarioBBDD = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == claveDTO.UsuarioId);

        if (usuarioBBDD != null)
        {
            //
            // Actualizo la ultima fecha de inicio de sesion.
            //
            //usuarioBBDD.FechaUltimaActualizacion = DateTime.Now;

            string _KeyBase64 = Cryptography.PassKey();


            string _pass = usuarioBBDD.Correo.ToLower() + claveDTO.ClaveNueva.Trim();
            usuarioBBDD.Clave = Cryptography.EncryptText(_pass, _KeyBase64); //cambio de clave.


            _context.Usuarios.Attach(usuarioBBDD);
            _context.Entry(usuarioBBDD).Property(x => x.Clave).IsModified = true;
            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        else
        {
            ModelState.AddModelError("", "Usuario o contraseña no valido.");


        }

        return View(claveDTO);

    }


    [HttpGet]
    public IActionResult ClaveReset(int id)
    {

        var claveDTO = new ClaveDTO()
        {
            UsuarioId = id,
            ClaveActual = "reset"
        };
        
        return View(claveDTO);
    }

    [HttpPost]
    public async Task<IActionResult> Autorizar(string clave)
    {

        try
        {
            
            if (string.IsNullOrWhiteSpace(clave))
            {
                return Ok(MessageDTO.Error("Debe ingresar una clave"));
            }


            var usuarioBBDD = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == this.UsuarioId);

            if (usuarioBBDD == null)
            {
                return Ok(MessageDTO.Error("El usuario no existe"));
            }

            string _KeyBase64 = Cryptography.PassKey();
            string _pass = usuarioBBDD.Correo.ToLower() + clave.Trim();

            string claveEncriptada = Cryptography.EncryptText(_pass, _KeyBase64);


            if (usuarioBBDD.Clave == claveEncriptada)
            {
                return Ok(MessageDTO.Ok("Clave ingresada con exito"));
            }
            else
            {
                return Ok(MessageDTO.Error("Verifique su contraseña"));
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);

            return BadRequest();
        }
    }
}