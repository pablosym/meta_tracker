namespace Tracker.DTOs;

public class TelefonoGuiaLogDTO
{
    public int Id { get; set; }
    public DateTime FechaRegistro { get; set; }
    public long NumGuia { get; set; }
    public int Cliente { get; set; }
    public long Afiliado { get; set; }
    public string Listapre { get; set; } = null!;
    public string? UsuarioRegistra { get; set; }
    public string? NombreAfiliado { get; set; } = string.Empty;
}
