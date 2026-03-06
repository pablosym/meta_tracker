using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class UsuarioDTO
    {
        public int Id { get; set; }


        [Display(Name = "CUIT")]
        public long? CUIT { get; set; }

        [Display(Name = "Cliente")]
        public int? ClienteCodigo { get; set; }

        public bool FlgAdmin { get; set; }
        

        [Display(Name = "Empresa")]
        [Required(ErrorMessage = "Debe ingresar una Empresa")]
        public int EmpresaId { get; set; }


        [Required(ErrorMessage = "Debe ingresar una clave")]
        [StringLength(100, ErrorMessage = "La {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 4)]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "La clave tiene que estar formada por un mínimo de 8 caracteres<br/>1 en mayúscula, 1 en minúscula<br/> y al menos 1 caracter especial")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;    

        [DataType(DataType.Password)]
        [Display(Name = "Confirmacion")]
        [Required(ErrorMessage = "Debe ingresar una confirmacion")]
        [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "La clave tiene que estar formada por un mínimo de 8 caracteres<br/>1 en mayúscula, 1 en minúscula<br/> y al menos 1 caracter especial")]
        [Compare("Password", ErrorMessage = "La clave y la confirmacion no son iguales.")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe ingresar un nombre")]
        [StringLength(140, ErrorMessage = "El {0} debe al menos tener {2} y un maximo de {1} caracteres de largo.", MinimumLength = 3)]
        [Display(Name = "Nombre", Prompt = "Nombre del Usuario")]
        [DataType(DataType.Text)]
        public string Nombre { get; set; } = string.Empty;


        [EmailAddress]
        [Required(ErrorMessage = "La direccion de correo no es correcta")]
        [Display(Name = "Correo", Prompt = "example@example.org")]
        [DataType(DataType.EmailAddress)]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Dar de Baja")]
        public bool Baja { get; set; }


        [Required(ErrorMessage = "Debe seleccionar un Rol")]
        [Display(Name = "Rol")]
        public List<int> IdRolesSel { get; set; } = new List<int>();

        [Required(ErrorMessage = "Debe seleccionar una Lista de Precio")]
        [Display(Name = "Listas de Precio")]
        public List<int> IdListaPreciosSel { get; set; } = new List<int>();
    }
}
