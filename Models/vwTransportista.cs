namespace Tracker.Models;

public class vwTransportista
{
    
    public long Idprovlogi { get; set; }
    public decimal Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public string? Direccion { get; set; }

    public string? Coordenadas { get; set; }
    public int? EstadoId { get; set; }

    public virtual Parametrico? Estado { get; set; }

}
