using Tracker.Interfaces; 
namespace Tracker.Models;
public partial class Usuario : IUsuario
{
    public virtual ICollection<UsuariosRole> UsuariosRoles { get; set; } = new List<UsuariosRole>();    

    public bool Baja  { get; set; } 
    public int Id  { get; set; } 
    public string Nombre {  get; set; } = String.Empty;
    public string Clave {  get; set; } = String.Empty;
    public string Correo { get; set; } = String.Empty;
    public bool FlgAdmin {  get; set; }  
    public DateTime? FechaUltimoIngreso {  get; set; } 
}
