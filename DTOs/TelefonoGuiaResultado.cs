using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs;

public class TelefonoGuiaResultado
{
    [Key]
    public long NumGuia { get; set; }
    public int Cliente { get; set; }
    public long Afiliado { get; set; }
    public string Listapre { get; set; }
    public string? TelefonoDomicili { get; set; }
    public string? TelefonoAfiliado { get; set; }
    public string TelefonoEstado { get; set; }
    // public string? NombreAfiliado { get; set; }
}