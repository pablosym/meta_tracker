using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs
{
    public class DespachoEnvioDTO
    {
        public int Id { get; set; }

        [Display(Name = "Número de Envío")]     
        public int EnvioNumero { get; set; }

        [Display(Name = "Código Transportista")]
        public long TransportistaCodigo { get; set; }

        [Display(Name = "Patente")]
        [DataType(DataType.Text)]
        public string Patente { get; set; } = String.Empty;

        [Display(Name = "Fecha de Envío")]
        [DataType(DataType.Date)]
        public DateTime FechaEnvio { get; set; }

        [Display(Name = "Fecha de Anulación")]
        [DataType(DataType.Date)]
        public DateTime? FechaAnulacion { get; set; }

        [Display(Name = "Anulado")]
        public bool Anulado { get; set; }

        [Display(Name = "Usuario Envío")]
        public int UsuarioEnvioId { get; set; }

        [Display(Name = "Usuario Anulación")]
        public int UsuarioAnulacionId { get; set; }

        [Display(Name = "Motivo Anulación")]
        [DataType(DataType.MultilineText)]
        public string? MotivoAnulacion { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string? Observaciones { get; set; }

        [Display(Name = "Chofer")]
        public int ChoferId { get; set; }
    }
}
