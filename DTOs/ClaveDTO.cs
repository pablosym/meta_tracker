using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class ClaveDTO
    {
        [Required(ErrorMessage = "Debe ingresar una clave")]
        [DataType(DataType.Password)]
        public string ClaveActual { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe ingresar una clave")]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "La clave tiene que estar formada por un mínimo de 8 caracteres<br/>1 en mayúscula, 1 en minúscula<br/> y al menos 1 caracter especial")]
        [DataType(DataType.Password)]
        public string ClaveNueva { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmacion")]
        [Required(ErrorMessage = "Debe ingresar una confirmacion")]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "La clave tiene que estar formada por un mínimo de 8 caracteres<br/>1 en mayúscula, 1 en minúscula<br/> y al menos 1 caracter especial")]
        [Compare("ClaveNueva", ErrorMessage = "La clave y la confirmacion no son iguales.")]
        public string ClaveConfirmacion { get; set; } = string.Empty;


        public int? UsuarioId { get; set; }
    }
}
