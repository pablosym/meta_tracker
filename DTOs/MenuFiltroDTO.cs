
namespace Tracker.DTOs

{
    public partial class MenuFiltroDTO
    {

        public int IdMenu { get; set; }
        public int? IdMenuPadre { get; set; }
        public string Item { get; set; } = String.Empty;    
        public string AspController { get; set; } = String.Empty;   
        public string AspAction { get; set; } = String.Empty;   
        public string Icono { get; set; } = String.Empty;   
        public int Orden { get; set; }
        public bool Baja { get; set; }

    }



}
