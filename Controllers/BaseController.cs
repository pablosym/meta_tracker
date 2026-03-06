using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Tracker.DTOs;
using Tracker.Helpers;
using Tracker.Models;

namespace Tracker.Controllers;

public class BaseController(Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : Controller
{
    public void SetMessage(MessageDTO messageDTO, object? obj)
    {
        TempData["mensaje"] = messageDTO.Value.Trim() + "|" + messageDTO.Status;

        if (obj != null)
        {
            TempData[Constants.TEMP_OBJ_PARA_CONFIRMACION] = JsonConvert.SerializeObject(obj);
        }
    }
    public void SetMessage(MessageDTO messageDTO)
    {
        SetMessage(messageDTO, null);
    }

    public Tracker_DevelContext _context = context;

    public IHttpContextAccessor _contextAccessor = contextAccessor;

    public int UsuarioId
    {
        get
        {
            var user = _contextAccessor.HttpContext?.User;
            return Security.GetIdUsuario(user);
        }
    }

    public string UsuarioCorreo
    {
        get
        {
            var user = _contextAccessor.HttpContext?.User;
            return Security.GetCorreoUsuario(user);
        }
    }

    public string UsuarioNombre
    {
        get
        {
            var user = _contextAccessor.HttpContext?.User;
            return Security.GetNombreUsuario(user);
        }
    }
    public async Task<bool> TienePermisos(int idRol)
    {
        return await _context.UsuariosRoles.AnyAsync(w => w.UsuarioId == this.UsuarioId && w.RolId == idRol);
    }

    public override void OnActionExecuting(ActionExecutingContext actionContext)
    {
        //Le paso al contexto el usuario y ID del usuario conectado para la auditoria.
        //_context.UserName = User.Identity.Name;
        //_context.UserId = Helpers.Security.GetIdUsuario(User);
    }

    protected string AppUrl(string? path = "/")
    {
        // Normalizamos el relativo: siempre empieza con "/"
        var rel = string.IsNullOrWhiteSpace(path) ? "/" : (path![0] == '/' ? path : "/" + path);
        var basePath = HttpContext?.Request.PathBase ?? PathString.Empty;

        // PathString.Add maneja bien barras y combina seguro
        return basePath.Add(rel).Value ?? rel;
    }

}
