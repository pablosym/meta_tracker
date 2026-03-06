using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class TransportistaDTO
    {
        public int Id { get; set; }

        public long Idprovlogi { get; set; }

        [Required(ErrorMessage = "Debe ingresar un Nombre")]
        [StringLength(250, ErrorMessage = "El {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 3)]
        [Display(Name = "Nombre", Prompt = "Nombre")]
        [DataType(DataType.Text)]
        public string Nombre { get; set; } = String.Empty;
        

        [Display(Name = "Codigo")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Debe ingresar un Codigo")]
        public decimal Codigo { get; set; }

        [Display(Name = "Direccion")]
        [DataType(DataType.Text)]
        public string? Direccion { get; set; }

        
        [Display(Name = "Estado")]
        public string? Estado { get; set; }

        public string? EstadoColor { get; set; }


        [Display(Name = "Estado")]
        [Required(ErrorMessage = "Debe ingresar un Estado")]
        public int EstadoId { get; set; }

        public bool Activo { get; set; }

        public string? Coordenadas{ get; set; }
    }
}
