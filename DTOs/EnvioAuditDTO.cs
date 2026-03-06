namespace Tracker.DTOs;

public class EnvioAuditDTO
{
    public int Id { get; set; }
    public long Envio { get; set; }
    public long Guia { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Estado { get; set; } = string.Empty;
    public string? EstadoColor { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string? Direccion { get; set; }

    public Guid? CodigoViaje { get; set; }

}