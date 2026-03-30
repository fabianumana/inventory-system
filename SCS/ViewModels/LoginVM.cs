using System.ComponentModel.DataAnnotations;

namespace SCS.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es obligatorio.")]
        [MinLength(8, ErrorMessage = "La contraseña no cumple con los requisitos o no coincide.")]
        public string Contrasena { get; set; }
    }
}
