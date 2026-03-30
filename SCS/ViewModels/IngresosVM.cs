using Microsoft.AspNetCore.Mvc.Rendering;
using SCS.Models;

namespace SCS.ViewModels
{
    public class IngresosVM
    {
        public IEnumerable<Ingresos> Ingresos { get; set; }
        public IEnumerable<SelectListItem> Partes { get; set; }
        public int? FiltroId { get; set; }
        public int? FiltroParteId { get; set; }
        public string FiltroParte { get; set; } = string.Empty;
    }
}
