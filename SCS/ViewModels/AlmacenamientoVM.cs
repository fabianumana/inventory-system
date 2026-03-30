using SCS.Models;

namespace SCS.ViewModels
{
    public class AlmacenamientoVM
    {
        public IEnumerable<Almacenamiento> Almacenamientos { get; set; }
        public int? FiltroId_parte { get; set; }
        public string? FiltroSuplidor { get; set; }
        public string? FiltroNumeroParte { get; set; }
    }
}
