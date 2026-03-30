using SCS.Models;

namespace SCS.ViewModels
{
    public class UsuarioRole
    {
        public int? UsuarioId { get; set; }
        public int? RolId { get; set; }
        public DateTime? RequestedAt { get; set; }
        public bool IsApproved { get; set; } = false;

        public Usuarios Usuario { get; set; }
        public Roles Rol { get; set; }
    }
}
