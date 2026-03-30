using SCS.Models;

namespace SCS.ViewModels
{
    public class FabricantesVM
    {
        public IEnumerable<Fabricantes> Fabricantes { get; set; }
        public int? FiltroId { get; set; }
        public int? FiltroParteId { get; set; }
        public string FiltroSuplidor { get; set; } = string.Empty;
        public bool? MostrarActivos { get; set; }
    }
}
