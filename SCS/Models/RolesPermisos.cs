using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class RolesPermisos
    {
        [Key]
        public int Id_role_permiso { get; set; }
        public int RolId { get; set; }
        public Roles Rol { get; set; }
        public int PermisoId { get; set; }
        public Permisos Permiso { get; set; }
    }
}
