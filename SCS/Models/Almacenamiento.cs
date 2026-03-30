using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class Almacenamiento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_almacenamiento { get; set; }

        [Required]
        public int Id_parte { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Parte")] 
        public string Part { get; set; }

        [Required]
        [StringLength(50)]
        public string Numero_parte { get; set; } 

        public string Descripcion { get; set; } 

        [Required]
        public int Cantidad_Disponible { get; set; } 

        public string Ubicacion { get; set; } 

        [Required]
        public DateTime? Fecha_Ingreso { get; set; } 

        public DateTime? Fecha_Vencimiento { get; set; }

        [Required]
        [StringLength(100)]
        public string Suplidor { get; set; } 

        public string Colateral { get; set; } 

        public byte[] Archivo { get; set; } 

        [StringLength(50)]
        public string MimeType { get; set; } 

        [Required]
        public bool Activo { get; set; } = true;
    }
}