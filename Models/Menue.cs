namespace Tracker.Models;

public partial class Menue
{
    public Menue()
    {
        MenuesRoles = new HashSet<MenuesRole>();
    }

    public int Id { get; set; }
    public int MenuPadreId { get; set; }
    public string Item { get; set; } = null!;
    public string AspController { get; set; } = null!;
    public string? AspAction { get; set; }
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Baja { get; set; } = false;

    public bool AccesoDirecto { get; set; } = false;
    public virtual ICollection<MenuesRole> MenuesRoles { get; set; }
}
