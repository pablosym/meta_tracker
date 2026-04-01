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


        /// <summary>
        /// Si es true, cuando se detectan múltiples teléfonos válidos se separa el envío por teléfono.
        /// Si es false, nunca se hace split. En ese caso, si una guía tiene más de un teléfono válido,
        /// se enviará sin teléfono a LogicTracker y se dejará auditoría de advertencia.
        /// </summary>
        public bool HabilitarSplitPorTelefono { get; set; } = true;

    }


    public class EntornoPruebas
    {
        public bool Activo { get; set; }

        public string Prefijo { get; set; } = string.Empty;

        public string URL { get; set; } = string.Empty;

    }
}
