using SCS.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class Roles
    {
        [Key]
        public int Id_roles { get; set; }

        [Required(ErrorMessage = "El campo Rol es obligatorio.")]
        public string? Rol { get; set; }

        public bool? Activo { get; set; } = true;

        public ICollection<UsuarioRole> UsuarioRoles { get; set; } = new List<UsuarioRole>();
        public ICollection<Usuarios> Perfiles { get; set; } = new List<Usuarios>();
        public ICollection<RolesPermisos> RolePermisos { get; set; } = new List<RolesPermisos>();
    }
}
