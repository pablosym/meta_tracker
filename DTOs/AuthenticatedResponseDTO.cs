
namespace Tracker.DTOs
{
    public class AuthenticatedResponseDTO
    {
        public string? Token { get; set; } = string.Empty;  
        public UsuarioDTO? Usuario { get; set; } = null;

        public List<MenuDTO> listMenuDTO { get; set; } = new List<MenuDTO>();

    }
}
