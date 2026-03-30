using SCS.Models;

namespace SCS.ViewModels
{
    public class UsuariosVM
    {
        public int? FiltroId { get; set; }
        public string? FiltroNombre { get; set; }
        public bool? MostrarInactivos { get; set; }
        public IEnumerable<Usuarios> Perfiles { get; set; }
    }
}
