using System.ComponentModel.DataAnnotations;

namespace SCS.ViewModels
{
    public class ContraseñaOlvidadaVM
    {
        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
        public string Email { get; set; }
    }
}
