using SCS.Models;

namespace SCS.ViewModels
{
    public class MovimientosVM
    {
        public IEnumerable<BitacoraMovimientos> Movimientos { get; set; }
        public int? FiltroId { get; set; }
        public int? FiltroPerfilId { get; set; }
        public string FiltroTipoAccion { get; set; } = string.Empty;
    }
}
