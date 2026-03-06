using Microsoft.EntityFrameworkCore;

namespace Tracker.Models;

[Keyless]
public class vwTelefonoGuia
{
    public long NumGuia { get; set; }
    public int Cliente { get; set; }
    public long Afiliado { get; set; }
    public string Listapre { get; set; } = string.Empty;

    public string? TelefonoDomicili { get; set; }
    public string? TelefonoAfiliado { get; set; }

    public string? NombreAfiliado { get; set; } = string.Empty;
    /// <summary>
    /// DOMICILI, AFILIADO_UNICO, AFILIADO_MULTIPLES o SIN_TELEFONO
    /// </summary>
    public string TelefonoEstado { get; set; } = null!;
}
