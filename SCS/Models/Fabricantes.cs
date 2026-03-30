using System.ComponentModel.DataAnnotations;
using SCS.Helpers;

namespace SCS.Models
{
    public class Fabricantes
    {
        [Key]
        public int Id_suplidor { get; set; }

        [Required(ErrorMessage = "El nombre del suplidor es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string Nombre_Suplidor { get; set; }

        [Required(ErrorMessage = "El contacto es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre del contacto no puede tener más de 100 caracteres.")]
        public string Contacto { get; set; }

        [Phone(ErrorMessage = "El teléfono no tiene un formato válido.")]
        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El campo Teléfono debe tener el formato ####-####.")]
        public string Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El Correo no es una dirección de correo válida.")]
        public string Correo { get; set; }

        [StringLength(250, ErrorMessage = "La dirección no puede tener más de 250 caracteres.")]
        public string Direccion { get; set; }

        [Required(ErrorMessage = "El país es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre del país no puede tener más de 100 caracteres.")]
        public string Pais { get; set; }

        [StringLength(250, ErrorMessage = "Las condiciones no pueden tener más de 250 caracteres.")]
        public string Condiciones { get; set; }

        [StringLength(100, ErrorMessage = "El nombre del colateral no puede tener más de 100 caracteres.")]
        public string Colateral { get; set; }

        [Required(ErrorMessage = "La fecha de registro es obligatoria.")]
        [DataType(DataType.Date, ErrorMessage = "El formato de la fecha no es válido.")]
        [CustomValidation(typeof(ValidationHelper), "ValidateFechaPasada")]
        public DateTime Fecha_Registro { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Objetos>? Partes { get; set; }
    }
}
