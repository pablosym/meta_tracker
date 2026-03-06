using System.ComponentModel.DataAnnotations;

namespace Tracker.DTOs;

public class EstadoDTO
{
    
    [Display(Name = "Estado")]
    public string? Estado { get; set; }

    [Display(Name = "Color")]
    public string? EstadoColor { get; set; }


    [Display(Name = "Estado")]
    public int? EstadoId { get; set; }

}
