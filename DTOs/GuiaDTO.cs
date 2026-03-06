using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Tracker.DTOs;

public class GuiaDTO  : EstadoDTO
{
    public Int64 Id { get; set; }

    public Int64 Numero { get; set; }

    public DateTime Fecha { get; set; }

    public int  ClienteCodigo { get; set; }

    public string ClienteNombre { get; set; } = null!;
    public string? ClienteDireccion { get; set; }
    
    public string? ClienteTelefono { get; set; }

    [NotMapped]
    public string? ClienteTelefonoOrigen { get; set; } 

    public decimal? DestinoLatitud { get; set; }

    public decimal? DestinoLongitud { get; set; }

    public long? Afiliado { get; set; }

    public string? AfiliadoNombre { get; set; }

    [NotMapped]
    public string Coordenadas
    {
        get
        {
            if (!DestinoLatitud.HasValue || !DestinoLongitud.HasValue)
                return string.Empty;

            var lat = DestinoLatitud.Value;
            var lon = DestinoLongitud.Value;

            // si viene 0,0 o lat/lon inválidas -> vacío
            if (lat == 0m && lon == 0m) return string.Empty;

            return string.Format(CultureInfo.InvariantCulture, "{0:0.######},{1:0.######}", lat, lon);
        }
    }

    // Devuelve una clave identificadora compuesta del cliente y su afiliado.
    // Formato: "ClienteCodigo-Afiliado"
    // Si el afiliado es 0, devuelve solo el código del cliente.
    [NotMapped]
    public string ClienteAfiliado
    {
        get
        {
            if (Afiliado == null || Afiliado == 0)
                return ClienteCodigo.ToString(CultureInfo.InvariantCulture);

            return $"{ClienteCodigo}-{Afiliado?.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    public int CantidadComprobantes { get; set; }

    public int RecordsTotal { get; set; }

    public int? EnvioId { get; set; }

}