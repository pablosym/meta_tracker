namespace Tracker.DTOs;

public class ArticuloDTO
{
    public Int64 Id { get; set; }

    public Int64 CabeceraComprobantesAfiliado { get; set; }
    
    public Int64 NumeroComprobante { get; set; }

    public Int64 ArticuloCodigo { get; set; }

    public string ArticuloDescripcion{ get; set; } = string.Empty;

    public decimal CantidadSolicitada { get; set; }

    public int RecordsTotal { get; set; }

    public string NroReceta { get; set; } = string.Empty;
    public string ListaPrecio { get; set; } = string.Empty;


    public string? Telefono { get; set; }
    public string? TelefonoOrigen { get; set; }

    public Int64 NumeroGuia { get; set; }
    public int ClienteCodigo { get; set; }

}