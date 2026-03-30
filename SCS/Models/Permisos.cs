using System.ComponentModel.DataAnnotations;

namespace SCS.Models
{
    public class Permisos
    {
        [Key]
        public int Id_permiso { get; set; }
        [Required(ErrorMessage = "El nombre del permiso es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre del permiso no puede tener más de 50 caracteres.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "El nombre del permiso solo debe contener letras y espacios.")]
        public string? NombrePermiso { get; set; }
        public bool? Activo { get; set; } = true;
    }
}
