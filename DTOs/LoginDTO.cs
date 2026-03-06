using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Debe ingresar una Correo")]
        [EmailAddress]
        
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar una Clave")]
        [DataType(DataType.Password)]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "La clave tiene que estar formada por un mínimo de 8 caracteres<br/>1 en mayúscula, 1 en minúscula<br/> y al menos 1 caracter especial")]
        public string Clave { get; set; } = string.Empty;


    }
}
