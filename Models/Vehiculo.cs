namespace Tracker.Models;
public class Vehiculo
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public string Patente { get; set; } = null!;
    public virtual Parametrico? Tipo { get; set; } 
    public int? TipoId { get; set; }
    public virtual Parametrico? Estado { get; set; } = null!;
    public int EstadoId { get; set; }

}
