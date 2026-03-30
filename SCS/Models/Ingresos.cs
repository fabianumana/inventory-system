using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCS.Models
{
    public class Ingresos
    {
        [Key]
        public int Id_transaccion_ing { get; set; }

        [Required(ErrorMessage = "El campo Parte es obligatorio.")]
        [ForeignKey("Parte")]
        public int Id_parte { get; set; }

        [Required(ErrorMessage = "El ID del perfil es obligatorio.")]
        public int Id_perfil { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder los 500 caracteres.")]
        public string Motivo { get; set; }

        [Required(ErrorMessage = "El departamento es obligatorio.")]
        public string Departamento { get; set; }

        [Required(ErrorMessage = "El número de serial es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de serial no puede exceder los 50 caracteres.")]
        public string Numero_serial { get; set; }

        [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime? Fecha_ingreso { get; set; }

        [Required(ErrorMessage = "El usuario de ingreso es obligatorio.")]
        public string Usuario_ingreso { get; set; }

        public bool Activo { get; set; } = true;

        [NotMapped]
        public Objetos Parte { get; set; }
    }
}
