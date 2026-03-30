using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SCS.Autorizacion;
using SCS.Helpers;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class EntradasSalidasController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;
        private readonly IConverter _pdfConverter;

        public EntradasSalidasController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService, IConverter pdfConverter)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
            _pdfConverter = pdfConverter;
        }

        // Método para mostrar la lista de entradas y salidas
        [AutorizacionAccesoAtributo("IndexEntraSal")]
        public async Task<IActionResult> Index(int? filtroId, string? FiltroTipoMovimiento)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var query = dbContext.Entradas_Salidas.Include(e => e.Perfil).AsQueryable();

            if (filtroId.HasValue)
            {
                query = query.Where(e => e.Id_ent_sal == filtroId);
            }

            if (!string.IsNullOrEmpty(FiltroTipoMovimiento))
            {
                query = query.Where(e => e.Usuario.Contains(FiltroTipoMovimiento) || e.Tipo_movimiento.Contains(FiltroTipoMovimiento));
            }

            var model = new EntradasSalidasVM
            {
                EntradasSalidas = await query.ToListAsync(),
                FiltroId = filtroId,
                FiltroTipoMovimiento = FiltroTipoMovimiento
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Visualización de Entradas y Salidas", "El usuario visualizó el reporte de entradas y salidas.", fechaAccion, horaAccion);
            }

            return View(model);
        }

        // Método para mostrar los detalles de una entrada o salida
        [AutorizacionAccesoAtributo("DetallesEntraSal")]
        public async Task<IActionResult> Details(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var entradas_Salidas = await dbContext.Entradas_Salidas
                .Include(e => e.Perfil)
                .FirstOrDefaultAsync(m => m.Id_ent_sal == id);

            if (entradas_Salidas == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Visualización de Detalles", $"Se visualizaron los detalles de la entrada/salida con ID {entradas_Salidas.Id_ent_sal}.", fechaAccion, horaAccion);
            }

            return View(entradas_Salidas);
        }

        // Método para exportar el reporte a Excel
        [AutorizacionAccesoAtributo("ExcelEntSal")]
        public async Task<IActionResult> ExportarExcel()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = await dbContext.Entradas_Salidas.Include(e => e.Perfil).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Usuario";
                worksheet.Cells[1, 3].Value = "Tipo Movimiento";
                worksheet.Cells[1, 4].Value = "Fecha Entrada";
                worksheet.Cells[1, 5].Value = "Hora Entrada";
                worksheet.Cells[1, 6].Value = "Fecha Salida";
                worksheet.Cells[1, 7].Value = "Hora Salida";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_ent_sal;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Usuario;
                    worksheet.Cells[i + 2, 3].Value = datos[i].Tipo_movimiento;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Fecha_entrada?.ToShortDateString();
                    worksheet.Cells[i + 2, 5].Value = datos[i].Hora_entrada?.ToString(@"hh\:mm\:ss");
                    worksheet.Cells[i + 2, 6].Value = datos[i].Fecha_salida?.ToShortDateString();
                    worksheet.Cells[i + 2, 7].Value = datos[i].Hora_salida?.ToString(@"hh\:mm\:ss");
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportación a Excel", "El usuario exportó el reporte de entradas y salidas a Excel.", fechaAccion, horaAccion);
                }

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteEntradasSalidas.xlsx");
            }
        }

        // Método para exportar el reporte a PDF
        [AutorizacionAccesoAtributo("PdfEntSal")]
        public async Task<IActionResult> ExportarPdf()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = await dbContext.Entradas_Salidas.Include(e => e.Perfil).ToListAsync();

            var html = @"
            <h1>Reporte de Entradas y Salidas</h1>
            <table border='1' cellpadding='5' cellspacing='0' width='100%'>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Usuario</th>
                        <th>Tipo Movimiento</th>
                        <th>Fecha Entrada</th>
                        <th>Hora Entrada</th>
                        <th>Fecha Salida</th>
                        <th>Hora Salida</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var item in datos)
                    {
                        html += $@"
                <tr>
                    <td>{item.Id_ent_sal}</td>
                    <td>{item.Usuario}</td>
                    <td>{item.Tipo_movimiento}</td>
                    <td>{item.Fecha_entrada?.ToShortDateString()}</td>
                    <td>{item.Hora_entrada?.ToString(@"hh\:mm\:ss")}</td>
                    <td>{item.Fecha_salida?.ToShortDateString()}</td>
                    <td>{item.Hora_salida?.ToString(@"hh\:mm\:ss")}</td>
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
            Orientation = DinkToPdf.Orientation.Portrait,
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

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportación a PDF", "El usuario exportó el reporte de entradas y salidas a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReporteEntradasSalidas.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                throw;
            }
        }
    }
}
