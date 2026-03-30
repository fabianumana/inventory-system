using SCS.Helpers;
using SCS.Models;
using System.ComponentModel.DataAnnotations;

namespace SCS.ViewModels
{
    public class ObjetosVM
    {
        public List<ObjetoDetalleVM> Objetos { get; set; } = new();
        public int? FiltroId { get; set; }
        public int? FiltroPerfilId { get; set; }
        public string? FiltroParte { get; set; } = string.Empty;
        public bool? MostrarActivas { get; set; }
    }

    public class ObjetoDetalleVM
    {
        public int Id_parte { get; set; }
        [Required(ErrorMessage = "El número de parte es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de parte no puede exceder los 50 caracteres.")]
        public string Numero_parte { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string Descripcion { get; set; }
        public string Fabricante { get; set; }
        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        public int SuplidorId { get; set; }
        [Required(ErrorMessage = "La cantidad disponible es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad disponible no puede ser negativa.")]
        public int Cantidad_Disponible { get; set; }
        [Required(ErrorMessage = "La ubicación es obligatoria.")]
        [StringLength(100, ErrorMessage = "La ubicación no puede exceder los 100 caracteres.")]
        public string Ubicacion { get; set; }
        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        public int Stock_Minimo { get; set; }
        [Required(ErrorMessage = "El stock máximo es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El stock máximo debe ser mayor a 0.")]
        public int Stock_Maximo { get; set; }
        [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Ingreso")]
        public DateTime Fecha_Ingreso { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Vencimiento")]
        [CustomValidation(typeof(ValidationHelper), nameof(ValidationHelper.ValidateFechaVencimiento))]
        public DateTime? Fecha_Vencimiento { get; set; }
        public byte[]? Archivo { get; set; }
        public string? MimeType { get; set; }
        public bool Activo { get; set; }
        public string ArchivoEstado => Archivo != null ? "Disponible" : "No disponible";
    }
}
