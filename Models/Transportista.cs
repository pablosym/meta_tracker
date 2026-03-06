namespace Tracker.Models;

public class Transportista
{
    public int Id { get; set; }
    public decimal Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public virtual Parametrico? Estado { get; set; } = null!;
    public int EstadoId { get; set; }

    public string? Coordenadas { get; set; }
}
