using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SCS.Autorizacion;
using SCS.Services;
using SCS.ViewModels;
using SCS.Models;
using System.Security.Claims;
using SCS.Helpers;

namespace SCS.Controllers
{
    public class MovimientosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly IConverter _converter;
        private readonly BitacorasService _bitacorasService;

        public MovimientosController(IDbContextFactory<Service> contextFactory, IConverter converter, BitacorasService bitacorasService)
        {
            _contextFactory = contextFactory;
            _converter = converter;
            _bitacorasService = bitacorasService;
        }

        // Método para visualizar el reporte de movimientos en la vista
        [AutorizacionAccesoAtributo("IndexMovimientos")]
        public async Task<IActionResult> Index(int? FiltroId, int? FiltroPerfilId, string FiltroTipoAccion)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var query = dbContext.Movimientos.AsQueryable();

            if (FiltroId.HasValue)
            {
                query = query.Where(m => m.Id_movimientos == FiltroId.Value);
            }

            if (FiltroPerfilId.HasValue)
            {
                query = query.Where(m => m.Id_perfi == FiltroPerfilId.Value);
            }

            if (!string.IsNullOrEmpty(FiltroTipoAccion))
            {
                query = query.Where(m => m.Tipo_accion.Contains(FiltroTipoAccion));
            }

            var movimientos = await query.ToListAsync();

            var viewModel = new MovimientosVM
            {
                FiltroId = FiltroId,
                FiltroPerfilId = FiltroPerfilId,
                FiltroTipoAccion = FiltroTipoAccion,
                Movimientos = movimientos
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Visualización de Movimientos", "El usuario visualizó el reporte de movimientos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        [AutorizacionAccesoAtributo("DetallesMovimientos")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();

            var movimiento = await dbContext.Movimientos
                .FirstOrDefaultAsync(m => m.Id_movimientos == id);

            if (movimiento == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Visualización de Detalles", $"Se visualizaron los detalles del movimiento con ID {movimiento.Id_movimientos}.", fechaAccion, horaAccion);
            }

            return View(movimiento);
        }

        // Método para exportar el reporte de movimientos a PDF
        [AutorizacionAccesoAtributo("PdfMovimientos")]
        public async Task<IActionResult> ExportToPdf()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var movimientos = await dbContext.Movimientos.ToListAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar PDF", "El usuario exportó el reporte de movimientos a PDF.", fechaAccion, horaAccion);
            }

            var html = @"
            <h1>Reporte de Movimientos</h1>
            <table border='1' cellpadding='5' cellspacing='0' width='100%'>
                <thead>
                    <tr>
                        <th>ID Movimiento</th>
                        <th>ID Perfil</th>
                        <th>Tipo Acción</th>
                        <th>Descripción</th>
                        <th>Fecha Acción</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var movimiento in movimientos)
                    {
                        html += $@"
                <tr>
                    <td>{movimiento.Id_movimientos}</td>
                    <td>{movimiento.Id_perfi}</td>
                    <td>{movimiento.Tipo_accion}</td>
                    <td>{movimiento.Descripcion}</td>
                    <td>{movimiento.Fecha_accion?.ToString("dd/MM/yyyy")}</td>
                </tr>";
                    }

                    html += @"
                </tbody>
            </table>";

            var converter = new SynchronizedConverter(new PdfTools());
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
            ColorMode = DinkToPdf.ColorMode.Color,
            Orientation = DinkToPdf.Orientation.Landscape,
            PaperSize = DinkToPdf.PaperKind.A4,
        },
                Objects = {
            new ObjectSettings() {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8" },
            }
        }
            };

            try
            {
                var pdf = converter.Convert(doc);
                return File(pdf, "application/pdf", "ReporteMovimientos.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el PDF: {ex.Message}");
                throw;
            }
        }

        // Método para exportar el reporte de movimientos a Excel
        [AutorizacionAccesoAtributo("ExcelMovimientos")]
        public async Task<IActionResult> ExportarExcel()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var movimientos = await dbContext.Movimientos.ToListAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar Excel", "El usuario exportó el reporte de movimientos a Excel.", fechaAccion, horaAccion);
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte de Movimientos");

                worksheet.Cells[1, 1].Value = "ID Movimiento";
                worksheet.Cells[1, 2].Value = "ID Perfil";
                worksheet.Cells[1, 3].Value = "Tipo Acción";
                worksheet.Cells[1, 4].Value = "Descripción";
                worksheet.Cells[1, 5].Value = "Fecha Acción";

                for (int i = 0; i < movimientos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = movimientos[i].Id_movimientos;
                    worksheet.Cells[i + 2, 2].Value = movimientos[i].Id_perfi;
                    worksheet.Cells[i + 2, 3].Value = movimientos[i].Tipo_accion;
                    worksheet.Cells[i + 2, 4].Value = movimientos[i].Descripcion;
                    worksheet.Cells[i + 2, 5].Value = movimientos[i].Fecha_accion?.ToString("dd/MM/yyyy");
                }

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteMovimientos.xlsx");
            }
        }
    }
}
