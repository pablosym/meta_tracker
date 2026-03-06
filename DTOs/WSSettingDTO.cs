using ServiceReference;

namespace Tracker.DTOs
{
    public class WSSettingDTO
    {

        public bool Activo { get; set; }
        public int Empresa { get; set; }

        public string URL { get; set; } = null!;

        public BaseOperativaWs BaseOperativa { get; set; } = null!;

        public EntornoPruebas EntornoPruebas { get; set; } = null!;
    }


    public class EntornoPruebas
    {
        public bool Activo { get; set; }

        public string Prefijo { get; set; } = string.Empty;

        public string URL { get; set; } = string.Empty;
      
    }
}
