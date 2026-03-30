using Microsoft.AspNetCore.Mvc.Rendering;
using SCS.Models;

namespace SCS.ViewModels
{
    public class EgresosVM
    {
        public IEnumerable<Egresos> Egresos { get; set; }
        public IEnumerable<SelectListItem> Partes { get; set; }
        public int? FiltroId { get; set; }
        public int? FiltroParteId { get; set; }
        public string FiltroParte { get; set; } = string.Empty;
    }
}
