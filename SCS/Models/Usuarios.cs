using SCS.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCS.Models
{
    public class Usuarios
    {
        [Key]
        public int Id_perfiles { get; set; }

        public int? Id_rol { get; set; }

        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]*$", ErrorMessage = "El campo Usuario solo puede contener letras.")]
        public string? User { get; set; }

        [RegularExpression(@"^\d{8}$", ErrorMessage = "El campo WWID debe tener exactamente 8 dígitos.")]
        public string? WWID { get; set; }

        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]*$", ErrorMessage = "El campo Apellidos solo puede contener letras.")]
        public string? Apellidos { get; set; }

        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El Correo no es una dirección de correo válida.")]
        public string? Correo { get; set; }

        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El campo Teléfono debe tener el formato ####-####.")]
        public string? Telefono { get; set; }

        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]*$", ErrorMessage = "El campo Departamento solo puede contener letras.")]
        public string? Departamento { get; set; }

        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]*$", ErrorMessage = "El campo Superior solo puede contener letras.")]
        public string? Superior { get; set; }

        [DataType(DataType.Password)]
        public string? Contrasena { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Contrasena", ErrorMessage = "La contraseña y confirmación no coinciden.")]
        public string? Confirmacion { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiration { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<UsuarioRole>? UsuarioRoles { get; set; }
        public ICollection<Objetos>? Partes { get; set; }
        public ICollection<Ingresos>? Ingreso { get; set; }
        public ICollection<Egresos>? Egreso { get; set; }
        public ICollection<BitacoraEntradasSalidas>? EntradasSalidas { get; set; }
        public ICollection<BitacoraMovimientos>? Movimientos { get; set; }

        [ForeignKey("Id_rol")]
        [NotMapped]
        public Roles? Rol { get; set; }
    }
}