using SCS.Models;

namespace SCS.ViewModels
{
    public class EntradasSalidasVM
    {
        public IEnumerable<BitacoraEntradasSalidas>? EntradasSalidas { get; set; }
        public int? FiltroId { get; set; }
        public string? FiltroTipoMovimiento { get; set; } = string.Empty;
    }
}
