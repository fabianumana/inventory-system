using SCS.Models;
using Microsoft.EntityFrameworkCore;
using SCS.ViewModels;

namespace SCS.Services
{
    public class BitacorasService
    {
        private readonly IDbContextFactory<Service> _contextFactory;

        public BitacorasService(IDbContextFactory<Service> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task RegistrarMovimientoAsync(int perfilId, string usuario, string tipoAccion, string descripcion, DateTime? fechaAccion, TimeSpan? horaAccion, int? parteId = null, int? ingresoId = null, int? egresoId = null)
        {
            using (var dbContext = _contextFactory.CreateDbContext())
            {
                var movimiento = new BitacoraMovimientos
                {
                    Id_perfi = perfilId,
                    Usuario = usuario,
                    Tipo_accion = tipoAccion,
                    Descripcion = descripcion,
                    Fecha_accion = fechaAccion,
                    Hora_accion = horaAccion,
                    ParteId_parte = parteId,
                    IngresosId_transaccion_ing = ingresoId,
                    EgresosId_transaccion_eg = egresoId
                };

                dbContext.Movimientos.Add(movimiento);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task RegistrarEntradaSalidaAsync(int perfilId, string usuario, string tipoMovimiento, DateTime? fechaEntrada, DateTime? fechaSalida, TimeSpan? horaEntrada, TimeSpan? horaSalida)
        {
            using (var dbContext = _contextFactory.CreateDbContext())
            {
                var entradaSalida = new BitacoraEntradasSalidas
                {
                    Id_perfil = perfilId,
                    Usuario = usuario,
                    Tipo_movimiento = tipoMovimiento,
                    Fecha_entrada = tipoMovimiento == "Entrada" ? fechaEntrada : null,
                    Fecha_salida = tipoMovimiento == "Salida" ? fechaSalida : null,
                    Hora_entrada = tipoMovimiento == "Entrada" ? horaEntrada : null,
                    Hora_salida = tipoMovimiento == "Salida" ? horaSalida : null
                };

                dbContext.Entradas_Salidas.Add(entradaSalida);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task RegistrarMovimientoYEntradaSalidaAsync(int perfilId, string usuario, string tipoAccion, string descripcion, string tipoMovimiento, DateTime? fechaEntrada, DateTime? fechaSalida, TimeSpan? horaEntrada, TimeSpan? horaSalida, DateTime? fechaAccion, TimeSpan? horaAccion)
        {
            using (var dbContext = _contextFactory.CreateDbContext())
            {
                var movimiento = new BitacoraMovimientos
                {
                    Id_perfi = perfilId,
                    Usuario = usuario,
                    Tipo_accion = tipoAccion,
                    Descripcion = descripcion,
                    Fecha_accion = fechaAccion,
                    Hora_accion = horaAccion
                };
                dbContext.Movimientos.Add(movimiento);

                var entradaSalida = new BitacoraEntradasSalidas
                {
                    Id_perfil = perfilId,
                    Usuario = usuario,
                    Tipo_movimiento = tipoMovimiento,
                    Fecha_entrada = fechaEntrada,
                    Fecha_salida = fechaSalida,
                    Hora_entrada = horaEntrada,
                    Hora_salida = horaSalida
                };
                dbContext.Entradas_Salidas.Add(entradaSalida);

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
