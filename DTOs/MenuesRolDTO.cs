using System.ComponentModel.DataAnnotations;


namespace Tracker.DTOs
{
    public partial class MenuesRolDTO
    {
        public int IdMenu { get; set; }

        
        [Range(0, int.MaxValue, ErrorMessage = "Debe seleccionar un Rol")]
        [Display(Name = "Rol")]
        public int IdRol { get; set; }

        [Display(Name = "Menu")]
        public List<int> listIdMenu { get; set; } = new List<int>();
    }
}
