using Tracker.Models;

namespace Tracker.Interfaces;

public interface IUsuario
{
    
    ICollection<UsuariosRole> UsuariosRoles { get; set; }
    bool Baja { get; set; }

    int Id { get; set; }
    
    string Nombre { get; set; }
    string Clave { get; set; } 
    string Correo { get; set; }
    bool FlgAdmin { get; set; }
    
    DateTime? FechaUltimoIngreso { get; set; }
}
