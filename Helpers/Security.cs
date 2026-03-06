using Tracker.DTOs;
using Newtonsoft.Json;

using System.Security.Claims;

namespace Tracker.Helpers;


public class Security
{

    public static string GetMenuAccesosDirectos(ClaimsPrincipal claimsPrincipal, string? basePath)
    {
        var listMenu = ObtenerMenuDelClaims(claimsPrincipal);
        return listMenu.CrearAccesosDirectos(basePath);
    }
    public static string GetMenu(ClaimsPrincipal? claimsPrincipal, string? basePath)
    {
        if (claimsPrincipal == null) return string.Empty;
        var listMenu = ObtenerMenuDelClaims(claimsPrincipal);
        return listMenu.Crear(basePath);
    }



    public static List<MenuDTO> ObtenerMenuDelClaims(ClaimsPrincipal claimsPrincipal)
    {

        List<MenuDTO> listMenu = new();
        var claims = claimsPrincipal.Claims.ToList();
        var menuEnClaims = claims.FirstOrDefault(x => x.Type.Contains("userdata"));

        if (menuEnClaims != null && !string.IsNullOrEmpty(menuEnClaims.Value))
        {
            listMenu = JsonConvert.DeserializeObject<List<MenuDTO>>(menuEnClaims.Value)!;
        }


        return listMenu;
    }

    public static bool TienePermisos(ClaimsPrincipal claimsPrincipal, int idRol)
    {
        if (claimsPrincipal == null)
            return false;


        if (claimsPrincipal?.Identity?.IsAuthenticated ?? false)
        {
            return claimsPrincipal.IsInRole(idRol.ToString());

        }

        return false;

    }


    public static int GetIdUsuario(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal != null && claimsPrincipal.Identity != null)
        {


            if (claimsPrincipal.Identity.IsAuthenticated)
            {

                var claims = claimsPrincipal.Claims.ToList();
                var idUsuarioClaims = claims.FirstOrDefault(x => x.Type.Contains("nameidentifier"));

                if (idUsuarioClaims != null && !string.IsNullOrEmpty(idUsuarioClaims.Value))
                {
                    return int.Parse(idUsuarioClaims.Value);
                }
                else
                    return 0;

            }
        }
        return 0;
    }


    public static int GetIdUsuario(IEnumerable<Claim> claims)
    {

        if (claims != null)
        {

            var idUsuarioClaims = claims.FirstOrDefault(x => x.Type.Contains("nameidentifier"));

            if (idUsuarioClaims != null && !string.IsNullOrEmpty(idUsuarioClaims.Value))
            {
                return int.Parse(idUsuarioClaims.Value);
            }
            else
                return 1;

        }
        return 1;
    }

    public static string GetAvatar(ClaimsPrincipal claimsPPal)
    {

        return "default.png";

        //if (claimsPPal != null)
        //{

        //    var imgAvatar = claimsPPal.Claims.FirstOrDefault(x => x.Type.Contains("surname"));

        //    if (imgAvatar != null && !string.IsNullOrEmpty(imgAvatar.Value))
        //    {
        //        return imgAvatar.Value;
        //    }
        //    else
        //        return "default.png";

        //}

    }

    /// <summary>
    /// Devuelve el TOKEN
    /// </summary>
    /// <param name="claimsPPal"></param>
    /// <returns></returns>
    public static string GetToken(ClaimsPrincipal claimsPPal)
    {

        if (claimsPPal != null)
        {


            var token = claimsPPal.Claims.FirstOrDefault(x => x.Type.Contains("authentication"));

            if (token != null && !string.IsNullOrEmpty(token.Value))
            {
                return token.Value;
            }
            else
                return "";

        }
        return "";
    }

    public static int GetClienteCodigo(ClaimsPrincipal claimsPPal)
    {

        if (claimsPPal != null)
        {

            var clienteCodigo = claimsPPal.Claims.FirstOrDefault(x => x.Type.Contains("actor"));

            if (clienteCodigo != null && !string.IsNullOrEmpty(clienteCodigo.Value))
            {
                return int.Parse(clienteCodigo.Value);
            }
            else
                return 0;

        }
        return 0;
    }


    public static int GetEmpresaId(ClaimsPrincipal claimsPPal)
    {

        if (claimsPPal != null)
        {

            var empresaId = claimsPPal.Claims.FirstOrDefault(x => x.Type.Contains("surname"));

            if (empresaId != null && !string.IsNullOrEmpty(empresaId.Value))
            {
                return int.Parse(empresaId.Value);
            }
            else
                return 0;

        }
        return 0;
    }


    public static string GetNombreUsuario(ClaimsPrincipal claimsPrincipal)
    {

        if (claimsPrincipal.Identity != null)
        {
            if (claimsPrincipal.Identity.IsAuthenticated)
            {
                return claimsPrincipal?.Identity?.Name ?? "";


            }
        }
        return "";
    }

    public static string GetCorreoUsuario(ClaimsPrincipal claimsPrincipal)
    {

        if (claimsPrincipal.Identity != null)
        {
            if (claimsPrincipal.Identity.IsAuthenticated)
            {

                var claims = claimsPrincipal.Claims.ToList();
                var correoClaims = claims.FirstOrDefault(x => x.Type.Contains("email"));

                if (correoClaims != null && !string.IsNullOrEmpty(correoClaims.Value))
                {
                    return correoClaims.Value;
                }
                else
                    return "";

            }
        }
        return "";
    }
}