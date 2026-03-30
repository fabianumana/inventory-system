using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCS.Models
{
    public class BitacoraEntradasSalidas
    {
        [Key]
        public int Id_ent_sal { get; set; }
        [Required]
        public int? Id_perfil { get; set; }
        public string? Usuario { get; set; }
        public string? Tipo_movimiento { get; set; }
        public DateTime? Fecha_salida { get; set; }
        public DateTime? Fecha_entrada { get; set; }
        public TimeSpan? Hora_salida { get; set; }
        public TimeSpan? Hora_entrada { get; set; }
        [ForeignKey("Id_perfil")]
        public Usuarios? Perfil { get; set; }
        public Ingresos? Ingresos { get; set; }
        public Egresos? Egresos { get; set; }
    }
}
