using SCS.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace SCS.Helpers
{
    public static class ValidationHelper
    {
        public static ValidationResult ValidateFechaPasada(DateTime fecha, ValidationContext context)
        {
            if (fecha > DateTime.Today)
            {
                return new ValidationResult("La fecha no puede ser futura.");
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateFechaVencimiento(DateTime? fechaVencimiento, ValidationContext context)
        {
            var instance = context.ObjectInstance as ObjetoDetalleVM;

            if (instance == null || instance.Fecha_Ingreso == default)
            {
                return ValidationResult.Success;
            }

            if (fechaVencimiento.HasValue && fechaVencimiento.Value <= instance.Fecha_Ingreso)
            {
                return new ValidationResult("La fecha de vencimiento debe ser posterior a la fecha de ingreso.");
            }

            return ValidationResult.Success;
        }
    }
}
