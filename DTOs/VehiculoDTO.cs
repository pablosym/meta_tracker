using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class VehiculoDTO
    {
        public int Id { get; set; }

     
        [Display(Name = "Observacion")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Debe ingresar una descripcion")]
        public string Descripcion { get; set; } = String.Empty;

        [Display(Name = "Patente")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Debe ingresar una patente")]
        public string Patente { get; set; } = String.Empty;

        public string? Tipo { get; set; } = String.Empty;

        [Display(Name = "Tipo Vehiculo")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de vehiculo")]
        [Required(ErrorMessage = "Debe seleccionar un tipo de vehiculo")]
        public int TipoId { get; set; }
        public string? Estado { get; set; } = String.Empty;

        [Display(Name = "Estado")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe seleccionar un estado")]
        [Required(ErrorMessage = "Debe seleccionar un estado")]
        public int EstadoId { get; set; }

        public string? EstadoColor { get; set; }

    }
}
