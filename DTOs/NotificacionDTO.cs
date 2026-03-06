
namespace Tracker.DTOs
{
    public class NotificacionDTO 
    {
        
        public string Mensaje { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public eTipoMensaje TipoMensaje { get; set; }

    }

    public enum eTipoMensaje
    {
        Ok = 1,
        Error = 2,
        Warninig = 3
    }
}

