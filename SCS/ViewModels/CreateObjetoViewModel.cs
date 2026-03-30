using Microsoft.AspNetCore.Mvc.Rendering;
using SCS.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace SCS.ViewModels
{
    public class CreateObjetoViewModel
    {
        [Required(ErrorMessage = "El número de parte es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de parte no puede exceder los 50 caracteres.")]
        public string Numero_parte { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        public int SuplidorId { get; set; }

        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        public string Fabricante { get; set; }

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
        [DataType(DataType.Upload)]
        public IFormFile Foto { get; set; }

        [ScaffoldColumn(false)]
        public List<SelectListItem> Suplidores { get; set; }
    }
}
