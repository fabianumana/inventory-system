using System.ComponentModel.DataAnnotations;

namespace SCS.ViewModels
{
    public class PerfilesVM
    {
        [Required(ErrorMessage = "El campo Usuario es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]+$", ErrorMessage = "El campo Usuario solo puede contener letras.")]
        public string Usuario { get; set; }

        [RegularExpression(@"^\d{8}$", ErrorMessage = "El campo WWID debe tener exactamente 8 dígitos.")]
        public string? WWID { get; set; }

        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$", ErrorMessage = "La contraseña debe contener al menos una letra mayúscula, una letra minúscula, un número y un carácter especial.")]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "El campo Confirmación es obligatorio.")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string Confirmacion { get; set; }

        [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z\sñÑáéíóúÁÉÍÓÚ]+$", ErrorMessage = "El campo Apellidos solo puede contener letras.")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El campo Teléfono debe tener el formato #### - ####.")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El campo Departamento es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "El campo Departamento solo puede contener letras.")]
        public string Departamento { get; set; }

        [Required(ErrorMessage = "El campo Manager es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "El campo Manager solo puede contener letras.")]
        public string Superior { get; set; }
    }
}
