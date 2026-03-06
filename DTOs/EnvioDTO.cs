using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DTOs;

public class EnvioDTO : EstadoDTO
{
    public Int64 Id { get; set; }

    public int? EnvioId { get; set; }


    [Display(Name = "Fecha Inicio")]
    [DataType(DataType.DateTime)]
    public DateTime? FechaInicio { get; set; } = DateTime.Now;

    [Display(Name = "Fecha Turno")]
    public DateTime? FechaTurno { get; set; } = DateTime.Now;

    [Display(Name = "Fecha Ultima Mod")]
    public DateTime? FechaUltimoMov { get; set; } = DateTime.Now;



   // [Required(ErrorMessage = "Debe ingresar un Codigo Transportista")]
    [Display(Name = "Codigo Transportista", Prompt = "Codigo Transportista")]
    public decimal? TransportistaCodigo { get; set; }

    [Display(Name = "Codigo Transportista Destino", Prompt = "Codigo Transportista Destino")]
    public decimal? TransportistaDestinoCodigo { get; set; }

    //[Required(ErrorMessage = "Debe ingresar un Transportista")]
    [StringLength(250, ErrorMessage = "El {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 3)]
    [Display(Name = "Transportista", Prompt = "Transportista")]
    public string? Transportista { get; set; } = string.Empty;


    // [StringLength(250, ErrorMessage = "El {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 3)]
    [Display(Name = "Transportista Destino", Prompt = "Transportista Destino")]
    public string? TransportistaDestino { get; set; } = string.Empty;


    [Display(Name = "Patente", Prompt = "Patente")]
    public string? Patente { get; set; }

    
    [Display(Name = "Chofer", Prompt = "Asigne un Chofer")]
    public string? Chofer { get; set; }

    public int? ChoferId { get; set; }

    public int? VehiculoId { get; set; } = null;

    
    [Display(Name = "Vehículo", Prompt = "Asigne un Vehículo")]
    public string? Vehiculo { get; set; }


    [Display(Name = "Tipo de Vehiculo", Prompt = "Tipo de Vehiculo")]
    public string? VehiculoTipo { get; set; }

    public int? VehiculoTipoId { get; set; }

    [Display(Name = "Cantidad de Guias", Prompt = "Cantidad de Guias")]
    public int? CantidadGuias { get; set; }
    
    public int? RecordsTotal { get; set; }

    public Int64 Numero { get; set; }

    
    [Display(Name = "Observación", Prompt = "Puede ingresar aqui alguna observación")]
    public string? Observaciones { get; set; } = String.Empty;


    [NotMapped]
    public List<EnvioDTO> listEnvios { get; set; } = new List<EnvioDTO>();

    public Guid? CodigoViaje { get; set; }
}