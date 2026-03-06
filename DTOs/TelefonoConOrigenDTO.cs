using Tracker.Helpers;
using static Tracker.Helpers.Constants;

namespace Tracker.DTOs;

public class TelefonoConOrigenDTO
{
    public string? Telefono { get; set; }
    public eTelefonoTablaOrigen Origen { get; set; }

    public string OrigenDescripcion => Origen.GetDescription();
}
