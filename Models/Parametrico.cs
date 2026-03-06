 namespace Tracker.Models;

public partial class Parametrico
{
    public Parametrico()
    {
        MenuesRoles = new HashSet<MenuesRole>();
        UsuariosRoles = new HashSet<UsuariosRole>();
    }

    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string? Valor { get; set; }
    public int Orden { get; set; }
    public bool Baja { get; set; }
    public string? Color { get; set; }
    public int ParametricosHeaderId { get; set; }
    public virtual ParametricosHeader ParametricosHeader { get; set; } = null!;
    public virtual ICollection<MenuesRole> MenuesRoles { get; set; }
    public virtual ICollection<UsuariosRole> UsuariosRoles { get; set; }
}