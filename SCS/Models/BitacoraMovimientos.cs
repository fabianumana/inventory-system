using SCS.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class BitacoraMovimientos
    {
        [Key]
        public int Id_movimientos { get; set; }
        [Required]
        public int? Id_perfi { get; set; }
        public string? Usuario { get; set; }
        public string? Tipo_accion { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? Fecha_accion { get; set; }
        public TimeSpan? Hora_accion{ get; set; }
        public int? ParteId_parte { get; set; } 
        public int? IngresosId_transaccion_ing { get; set; } 
        public int? EgresosId_transaccion_eg { get; set; }
        public Ingresos? Ingresos { get; set; }
        public Egresos? Egresos { get; set; }
        public Usuarios? Perfil { get; set; }
        public BitacoraEntradasSalidas? EntradasSalidas { get; set; }
        public Objetos? Parte { get; set; }
        public Roles? Rol { get; set; }
        public Fabricantes? Suplidor { get; set; }
    }
}
