using ServiceReference;

namespace Tracker.DTOs
{
    public class AuditSettingDTO
    {
        public int LimpiarAlosDias { get; set; } = 30;
        public bool SoloErrores { get; set; } = true;
    }
}
