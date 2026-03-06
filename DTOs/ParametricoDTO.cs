using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public partial class ParametricoDTO
    {

        public int Id { get; set; }
        
        [Display(Name = "Dar de Baja")]
        public bool Baja { get; set; }

        [Display(Name = "Clave")]
        [Range(0, int.MaxValue, ErrorMessage = "Debe seleccionar una Clave")]
        [Required(ErrorMessage = "Debe seleccionar una Clave")]
        public int ParametricosHeaderId { get; set; }


        [Required(ErrorMessage = "Debe seleccionar un Codigo")]
        [StringLength(50)]
        [Display(Name = "Codigo")]
        public string Codigo { get; set; } = String.Empty;

        [Required(ErrorMessage = "Debe seleccionar una Observacion")]
        [StringLength(100)]
        [Display(Name = "Observacion")]
        public string Descripcion { get; set; } = String.Empty;

        [Display(Name = "Valor")]
        public string? Valor { get; set; }


        [Display(Name = "Orden")]
        public int? Orden { get; set; }

        [Display(Name = "Color")]
        public string? Color { get; set; }

    }
}
