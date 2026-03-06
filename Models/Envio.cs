using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Models;

public partial class Envio
{
    public int Id { get; set; }
    public Int64 Numero { get; set; }
    public decimal TransportistaCodigo { get; set; }
    public decimal? TransportistaDestinoCodigo { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaTurno { get; set; }
    public DateTime? FechaUltimoMov { get; set; }

    public Guid? CodigoViaje { get; set; }

    public int? EstadoId { get; set; }
    public int UsuarioId { get; set; }
    public int? UsuarioUltimoMovId { get; set; }
    public string? Observaciones { get; set; }
    public int? ChoferId { get; set; }

    public int? VehiculoId { get; set; }

    public virtual Chofer? Chofer { get; set; } 
    public virtual Parametrico? Estado { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Usuario? UsuarioUltimoMov { get; set; }

    public virtual Vehiculo? Vehiculo { get; set; }

    [NotMapped]
    public virtual vwTransportista? TransportistaDestino { get; set; }   


    [NotMapped]
    public virtual vwTransportista? Transportista { get; set; }

    public virtual HashSet<EnvioGuia> Guias { get; set; } = [];
    
}
