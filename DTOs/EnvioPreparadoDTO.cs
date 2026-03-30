using Tracker.Models;

namespace Tracker.DTOs;
public class EnvioPreparadoDTO
{
    public int EnvioId { get; set; }
    public long NumeroEnvio { get; set; }
    public Guid CodigoViaje { get; set; }
    public string? TelefonoGrupo { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaTurno { get; set; }

    public string? DireccionDestino { get; set; }
    public string? CoordenadasDestino { get; set; }

    public vwTransportista? Transportista { get; set; }
    public vwTransportista? TransportistaDestino { get; set; }

    public Chofer? Chofer { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public List<GuiaPreparadaDTO> Guias { get; set; } = [];
}

public class GuiaPreparadaDTO
{
    public long NumeroGuia { get; set; }
    public DateTime? Fecha { get; set; }
    public int EstadoId { get; set; }

    public long ClienteCodigo { get; set; }
    public string? ClienteAfiliado { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteDireccion { get; set; }
    public string? Coordenadas { get; set; }

    public long? AfiliadoId { get; set; }
    public string? AfiliadoNombre { get; set; }
    public string? Telefono { get; set; }

    public List<RemitoPreparadoDTO> Remitos { get; set; } = [];
}

public class RemitoPreparadoDTO
{
    public long NumeroRemito { get; set; }
    public List<InsumoPreparadoDTO> Insumos { get; set; } = [];
}

public class InsumoPreparadoDTO
{
    public long ArticuloCodigo { get; set; }
    public string? ArticuloDescripcion { get; set; }
    public int Cantidad { get; set; }
}