namespace Tracker.Models;

public partial class ParametricosHeader
{
    public ParametricosHeader()
    {
        Parametricos = new HashSet<Parametrico>();
    }

    public int Id { get; set; }
    public string Descripcion { get; set; } = null!;
    public bool Baja { get; set; }

    public virtual ICollection<Parametrico> Parametricos { get; set; }
}
