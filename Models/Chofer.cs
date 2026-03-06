namespace Tracker.Models;

public class Chofer
{
    public int Id { get; set; }

    public string ApellidoNombre { get; set; } = String.Empty;
    public string Legajo { get; set; } = String.Empty;
    public string? Observacion { get; set; } 
    public string? Telefono { get; set; }
    public string Dni { get; set; } = String.Empty;
    public int EstadoId { get; set; }
    public virtual Parametrico Estado { get; set; } = null!;

    public bool Baja { get; set; }
}
