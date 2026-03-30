using Microsoft.EntityFrameworkCore;
using SCS.Models;
using System;
using System.Threading.Tasks;

namespace SCS.Services
{
    public class AlmacenamientoService
    {
        private readonly IDbContextFactory<Service> _contextFactory;

        public AlmacenamientoService(IDbContextFactory<Service> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task ActualizarAlmacenamiento(int idParte, int cantidad, bool esIngreso)
        {
            if (cantidad <= 0)
            {
                throw new Exception("La cantidad debe ser mayor a cero.");
            }

            using var dbContext = _contextFactory.CreateDbContext();
            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var parte = await dbContext.Objetos
                    .Include(o => o.Suplidor)
                    .FirstOrDefaultAsync(o => o.Id_parte == idParte);

                if (parte == null)
                {
                    throw new Exception("La parte seleccionada no existe.");
                }

                if (!esIngreso && !parte.Activo)
                {
                    throw new Exception("La parte seleccionada no está activa.");
                }

                var almacenamiento = await dbContext.Almacenamientos.FirstOrDefaultAsync(a => a.Id_parte == idParte);

                if (almacenamiento == null)
                {
                    if (!esIngreso)
                    {
                        throw new Exception("No hay suficiente stock disponible para esta parte.");
                    }

                    almacenamiento = new Almacenamiento
                    {
                        Id_parte = idParte,
                        Part = parte.Nombre,
                        Numero_parte = parte.Numero_parte,
                        Descripcion = parte.Descripcion,
                        Cantidad_Disponible = cantidad,
                        Ubicacion = parte.Ubicacion,
                        Fecha_Ingreso = DateTime.Now,
                        Fecha_Vencimiento = parte.Fecha_Vencimiento,
                        Colateral = parte.Suplidor?.Colateral,
                        Suplidor = parte.Suplidor?.Nombre_Suplidor,
                        Archivo = parte.Archivo,
                        MimeType = parte.MimeType,
                        Activo = true
                    };
                    dbContext.Almacenamientos.Add(almacenamiento);
                }
                else
                {
                    if (!esIngreso && almacenamiento.Cantidad_Disponible < cantidad)
                    {
                        throw new Exception("No hay suficiente stock disponible para esta parte.");
                    }

                    almacenamiento.Cantidad_Disponible += esIngreso ? cantidad : -cantidad;

                    if (esIngreso)
                    {
                        almacenamiento.Fecha_Ingreso = DateTime.Now;
                    }

                    dbContext.Almacenamientos.Update(almacenamiento);
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}