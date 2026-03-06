using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class ChoferDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe ingresar un Apellido y Nombre")]
        [StringLength(250, ErrorMessage = "El {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 3)]
        [Display(Name = "Apellido y Nombre", Prompt = "Apellido y Nombre")]
        [DataType(DataType.Text)]
        public string ApellidoNombre { get; set; } = String.Empty;
        

        [Display(Name = "Legajo")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Debe ingresar un legajo")]
        public string Legajo { get; set; } = String.Empty;

        [Display(Name = "Observacion")]
        [DataType(DataType.MultilineText)]
        [Required(ErrorMessage = "Debe ingresar una Observacion")]
        public string? Observacion { get; set; }

        [Display(Name = "Telefono")]
        [DataType(DataType.PhoneNumber)]
        public string? Telefono { get; set; }

        [Display(Name = "DNI")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Debe ingresar un DNI")]
        public string Dni { get; set; } = String.Empty;

        [Display(Name = "Estado")]
        public string? Estado { get; set; }

        public string? EstadoColor { get; set; }


        [Display(Name = "Estado")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe seleccionar un estado")]
        [Required(ErrorMessage = "Debe seleccionar un estado")]
        public int EstadoId { get; set; }


        public bool Baja { get; set; }

    }
}
