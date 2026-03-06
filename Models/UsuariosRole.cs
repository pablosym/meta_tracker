namespace Tracker.Models;

public partial class UsuariosRole
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int RolId { get; set; }

    public virtual Parametrico Rol { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}
