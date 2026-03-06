namespace Tracker.Models;

public partial class EnvioGuia
{
    public int Id { get; set; }

    public Int64 Numero { get; set; }

    public int EnvioId { get; set; }

    public DateTime Fecha { get; set; }

    public int? EstadoId { get; set; }
    
    public virtual Parametrico? Estado { get; set; }

    public virtual Envio Envio { get; set; } = null!;

}
