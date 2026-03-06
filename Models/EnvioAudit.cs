namespace Tracker.Models;

public class EnvioAudit
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public Int64 Envio { get; set; }
    public long Guia { get; set; }
    public string Usuario { get; set;} = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public virtual Parametrico? Estado { get; set; } = null!;
    public int EstadoId { get; set; }

    public string? Direccion { get; set; }

    public Guid? CodigoViaje { get; set; }
}
