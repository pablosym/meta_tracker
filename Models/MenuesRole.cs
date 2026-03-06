namespace Tracker.Models;

public partial class MenuesRole
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public int RolId { get; set; }
    public virtual Menue Menu { get; set; } = null!;
    public virtual Parametrico Rol { get; set; } = null!;
}
