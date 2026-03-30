using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class Objetos
    {
        [Key]
        public int Id_parte { get; set; }
        [Required]
        public string Numero_parte { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Fabricante { get; set; }
        public int Cantidad_Disponible { get; set; }
        public string Ubicacion { get; set; }
        public int Stock_Minimo { get; set; }
        public int Stock_Maximo { get; set; }
        public DateTime Fecha_Ingreso { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public int SuplidorId { get; set; }
        public Fabricantes Suplidor { get; set; }
        public byte[] Archivo { get; set; } 
        public string MimeType { get; set; }
        public bool Activo { get; set; } = true;

        public ICollection<Ingresos> Ingresos { get; set; } = new List<Ingresos>();
        public ICollection<Egresos> Egresos { get; set; } = new List<Egresos>();
    }
}
