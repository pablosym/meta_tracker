
using System.Text;

namespace Tracker.DTOs;


public partial class MenuDTO
{

    public int IdMenu { get; set; }
    public int? IdMenuPadre { get; set; }
    public string Item { get; set; } = String.Empty;
    public string AspController { get; set; } = String.Empty;
    public string AspAction { get; set; } = String.Empty;
    public string Icono { get; set; } = String.Empty;
    public int Orden { get; set; }
    public bool? Baja { get; set; }

    public bool AccesoDirecto { get; set; } = false;
}


[Serializable]
public static class MenuDTOExtensionMethods
{
    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim();
        if (!s.StartsWith("/")) s = "/" + s;
        while (s.Contains("//")) s = s.Replace("//", "/");
        if (s.Length > 1 && s.EndsWith("/")) s = s.TrimEnd('/');
        return s;
    }

    private static string Combine(string basePath, string rel)
    {
        var b = Normalize(basePath);
        var r = Normalize(rel);
        if (string.IsNullOrEmpty(b)) return r;
        if (r.StartsWith(b + "/")) return r; // ya viene con base
        return (b + r).Replace("//", "/");
    }

    public static string Crear(this List<MenuDTO> listMenu, string? basePath)
    {
        if (listMenu == null) return string.Empty;
        var bp = basePath ?? "";
        var sb = new StringBuilder();

        var Padres = listMenu.FindAll(x => x.IdMenuPadre == x.IdMenu);
        bool esPrimeraVuelta = true;

        foreach (var item in Padres)
        {
            sb.Append(esPrimeraVuelta
                ? $"<li class='nav-item mt{item.AspController}'> "
                : "<li class='nav-item has-treeview'>" + $"<li class='nav-item mt{item.AspController}'> ");
            esPrimeraVuelta = false;

            var icono = string.IsNullOrEmpty(item.Icono) ? "" : $"<i class='nav-icon {item.Icono}'></i>";
            sb.Append($"<a href='#' class='nav-link namt{item.AspController}{(string.IsNullOrEmpty(item.AspAction) ? "" : item.AspAction.ToLower())}'>{icono}<p>{item.Item}<i class='fas fa-angle-left right'></i></p></a>");
            sb.Append("<ul class='nav nav-treeview'>");

            var hijos = listMenu.FindAll(x => x.IdMenuPadre == item.IdMenu && x.IdMenu != item.IdMenu);
            SubMenu(hijos, listMenu, sb, bp);

            sb.Append("</ul></li>");
        }

        return sb.ToString();
    }

    private static string SubMenu(List<MenuDTO> subMenu, List<MenuDTO> listMenu, StringBuilder sb, string basePath)
    {
        foreach (var item in subMenu)
        {
            var icono = string.IsNullOrEmpty(item.Icono) ? "" : $"<i class='nav-icon {item.Icono}'></i>";
            var ctrl = (item.AspController ?? "").ToLowerInvariant();
            var act = (item.AspAction ?? "").ToLowerInvariant();

            if (string.IsNullOrEmpty(item.AspAction))
            {
                sb.Append($"<li class='nav-item mt{item.AspController}'> ");
                var hrefPadre = Combine(basePath, "/" + ctrl + (string.IsNullOrEmpty(act) ? "" : ("/" + act)));
                sb.Append($"<a href='{hrefPadre}' class='nav-link namt{item.AspController}{act}'>{icono}<p>{item.Item}<i class='fas fa-angle-left right'></i></p></a>");

                sb.Append("<ul class='nav nav-treeview'>");
                var hijos = listMenu.FindAll(x => x.IdMenuPadre == item.IdMenu && x.IdMenu != item.IdMenu);
                SubMenu(hijos, listMenu, sb, basePath);
                sb.Append("</ul></li>");
            }
            else
            {
                var href = Combine(basePath, "/" + ctrl + "/" + act);
                sb.Append($"<li class='nav-item'><a href='{href}' class='nav-link namt{item.AspController}{act}'>{icono}<p>{item.Item}</p></a></li>");
            }
        }
        return sb.ToString();
    }

    public static string CrearAccesosDirectos(this List<MenuDTO> listMenu, string? basePath)
    {
        if (listMenu == null) return string.Empty;
        var bp = basePath ?? "";

        var accesosDirectos = listMenu.FindAll(x => x.AccesoDirecto);
        if (accesosDirectos == null || accesosDirectos.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("<div class='row'>");

        foreach (var item in accesosDirectos)
        {
            var menuPadre = listMenu.FirstOrDefault(x => x.IdMenu == item.IdMenuPadre);
            if (menuPadre == null) continue;

            var ctrl = (item.AspController ?? "").ToLowerInvariant();
            var act = (item.AspAction ?? "").ToLowerInvariant();
            var href = Combine(bp, "/" + ctrl + "/" + act);

            sb.Append("<div class='col-lg-3 col-md-4 col-sm-6 col-xs-12 mb-3'>");
            sb.Append($"<a href='{href}' class='text-dark'><div class='info-box elevation-2' style='max-height: 200px; overflow-y: auto;'>");
            sb.Append($"<span class='info-box-icon text-blue'><i class='{item.Icono}'></i></span>");
            sb.Append("<div class='info-box-content'>");
            sb.Append($"<span class='info-box-text'>{menuPadre.Item}</span>");
            sb.Append("<span class='info-box-number'>");
            sb.Append(item.Item);
            sb.Append("<small></small></span></div></div></a></div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}
