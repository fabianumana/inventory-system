namespace SCS.ViewModels
{
    public class RolesPermisoVM
    {
        public int RolId { get; set; }
        public string RolNombre { get; set; }
        public List<PermisosVM> Permisos { get; set; } = new List<PermisosVM>();
        public List<int> PermisosSeleccionados { get; set; } = new List<int>();
    }
}
