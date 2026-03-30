using DinkToPdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SCS.Autorizacion;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using SCS.Helpers;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;

        public UsuariosController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
        }

        // GET: Usuarios/Index
        [AutorizacionAccesoAtributo("IndexUsuarios")]
        public async Task<IActionResult> Index(int? filtroId, string filtroNombre, bool? mostrarInactivos)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var query = dbContext.Perfiles.AsQueryable();

            if (!mostrarInactivos.HasValue || !mostrarInactivos.Value)
            {
                query = query.Where(p => p.Activo);
            }

            if (filtroId.HasValue)
            {
                query = query.Where(p => p.Id_perfiles == filtroId.Value);
            }

            if (!string.IsNullOrEmpty(filtroNombre))
            {
                query = query.Where(p => p.User.Contains(filtroNombre));
            }

            var perfiles = await query.ToListAsync();

            var viewModel = new UsuariosVM
            {
                FiltroId = filtroId,
                FiltroNombre = filtroNombre,
                MostrarInactivos = mostrarInactivos,
                Perfiles = perfiles
            };

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Index", "Se accedió al listado de perfiles.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        // GET: Usuarios/Details/5
        [AutorizacionAccesoAtributo("DetallesUsuarios")]
        public async Task<IActionResult> Details(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var perfil = await dbContext.Perfiles.FirstOrDefaultAsync(m => m.Id_perfiles == id);
            if (perfil == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Visuzlizacion", "Se vizuliso un perfil.", fechaAccion, horaAccion);
            }

            return View(perfil);
        }

        // GET: Usuarios/Edit/5
        [AutorizacionAccesoAtributo("EditarUsuarios")]
        public async Task<IActionResult> Edit(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var perfil = await dbContext.Perfiles.FindAsync(id);
            if (perfil == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Edicion", "Se accedió al apartado de edicion de perfiles.", fechaAccion, horaAccion);
            }

            return View(perfil);
        }

        // POST: Usuarios/Edit/5
        [AutorizacionAccesoAtributo("EditarUsuarios")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_perfiles,User,WWID,Apellidos,Correo,Telefono,Departamento,Superior")] Usuarios perfil)
        {
            if (id != perfil.Id_perfiles)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    if (entry.Value.ValidationState == ModelValidationState.Invalid)
                    {
                        var errors = string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage));
                        Console.WriteLine($"Error en {entry.Key}: {errors}");
                    }
                }
                return View(perfil);
            }

            using var dbContext = _contextFactory.CreateDbContext();

            var perfilOriginal = await dbContext.Perfiles.FindAsync(id);
            if (perfilOriginal == null)
            {
                return NotFound();
            }

            perfilOriginal.User = perfil.User ?? perfilOriginal.User;
            perfilOriginal.WWID = perfil.WWID ?? perfilOriginal.WWID;
            perfilOriginal.Apellidos = perfil.Apellidos ?? perfilOriginal.Apellidos;
            perfilOriginal.Correo = perfil.Correo ?? perfilOriginal.Correo;
            perfilOriginal.Telefono = perfil.Telefono ?? perfilOriginal.Telefono;
            perfilOriginal.Departamento = perfil.Departamento ?? perfilOriginal.Departamento;
            perfilOriginal.Superior = perfil.Superior ?? perfilOriginal.Superior;

            try
            {
                await dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el perfil: {ex.Message}");
                return View(perfil);
            }
        }

        // GET: Usuarios/Delete/5
        [AutorizacionAccesoAtributo("EliminarUsuarios")]
        public async Task<IActionResult> Disable(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null || id <= 0)
            {
                return NotFound(); 
            }

            var perfil = await dbContext.Perfiles.FirstOrDefaultAsync(m => m.Id_perfiles == id);
            if (perfil == null)
            {
                return NotFound(); 
            }

            return View(perfil); 
        }

        //Eliminar POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarUsuarios")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            Console.WriteLine($"ID recibido en DisableConfirmed: {id}");
            if (id <= 0)
            {
                ModelState.AddModelError("", "ID inválido proporcionado.");
                return RedirectToAction(nameof(Index));
            }

            using var dbContext = _contextFactory.CreateDbContext();

            try
            {
                var perfil = await dbContext.Perfiles.FindAsync(id);
                if (perfil == null)
                {
                    ModelState.AddModelError("", $"El perfil con ID {id} no fue encontrado.");
                    return RedirectToAction(nameof(Index));
                }

                perfil.Activo = false;
                dbContext.Entry(perfil).State = EntityState.Modified;

                await dbContext.SaveChangesAsync();

                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    await _bitacorasService.RegistrarMovimientoAsync(
                        perfilId,
                        User.Identity.Name,
                        "Deshabilitar",
                        $"Se deshabilitó el perfil con ID {id}.",
                        fechaAccion,
                        horaAccion
                    );
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "El perfil fue modificado por otro usuario mientras intentabas deshabilitarlo.");
            }
            catch (DbUpdateException dbEx)
            {
                ModelState.AddModelError("", $"Error al actualizar la base de datos: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ocurrió un error inesperado: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PerfilesExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Perfiles.Any(e => e.Id_perfiles == id);
        }

        [AutorizacionAccesoAtributo("ReportesUsuarios")]
        public async Task<IActionResult> ReportePerfiles(int? filtroId, string? filtroNombre, bool? mostrarInactivos)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var query = dbContext.Perfiles.AsQueryable(); 

            if (filtroId.HasValue)
            {
                query = query.Where(p => p.Id_perfiles == filtroId.Value);
            }

            if (!string.IsNullOrEmpty(filtroNombre))
            {
                query = query.Where(p => p.User.Contains(filtroNombre));
            }

            if (!mostrarInactivos.HasValue || !mostrarInactivos.Value)
            {
                query = query.Where(p => p.Activo);
            }

            var perfiles = await query.ToListAsync();

            var viewModel = new UsuariosVM
            {
                FiltroId = filtroId,
                FiltroNombre = filtroNombre,
                MostrarInactivos = mostrarInactivos,
                Perfiles = perfiles
            };

            return View(viewModel);
        }

        //Metodo para exportar a Excel
        [AutorizacionAccesoAtributo("ExcelUsuarios")]
        public IActionResult ExportarExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Perfiles.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Usuario";
                worksheet.Cells[1, 3].Value = "WWID";
                worksheet.Cells[1, 4].Value = "Apellidos";
                worksheet.Cells[1, 5].Value = "Correo";
                worksheet.Cells[1, 6].Value = "Teléfono";
                worksheet.Cells[1, 7].Value = "Departamento";
                worksheet.Cells[1, 8].Value = "Superior";
                worksheet.Cells[1, 9].Value = "Contraseña";
                worksheet.Cells[1, 10].Value = "Confirmación";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_perfiles;
                    worksheet.Cells[i + 2, 2].Value = datos[i].User;
                    worksheet.Cells[i + 2, 3].Value = datos[i].WWID;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Apellidos;
                    worksheet.Cells[i + 2, 5].Value = datos[i].Correo;
                    worksheet.Cells[i + 2, 6].Value = datos[i].Telefono;
                    worksheet.Cells[i + 2, 7].Value = datos[i].Departamento;
                    worksheet.Cells[i + 2, 8].Value = datos[i].Superior;
                    worksheet.Cells[i + 2, 9].Value = datos[i].Contrasena;
                    worksheet.Cells[i + 2, 10].Value = datos[i].Confirmacion;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a Excel", "Se exportó el reporte de perfiles a Excel.", fechaAccion, horaAccion);
                }

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReportePerfiles.xlsx");
            }
        }

        //Metodo para exportar a PDF
        [AutorizacionAccesoAtributo("PdfUsuarios")]
        public IActionResult ExportarPdf()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Perfiles.ToList();

            var html = @"
            <h1>Reporte de Perfiles</h1>
            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Usuario</th>
                        <th>WWID</th>
                        <th>Apellidos</th>
                        <th>Correo</th>
                        <th>Teléfono</th>
                        <th>Departamento</th>
                        <th>Superior</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var perfil in datos)
                    {
                        html += $@"
                <tr>
                    <td>{perfil.Id_perfiles}</td>
                    <td>{perfil.User}</td>
                    <td>{perfil.WWID}</td>
                    <td>{perfil.Apellidos}</td>
                    <td>{perfil.Correo}</td>
                    <td>{perfil.Telefono}</td>
                    <td>{perfil.Departamento}</td>
                    <td>{perfil.Superior}</td>
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
            PaperSize = DinkToPdf.PaperKind.A4
        },
                Objects = {
            new ObjectSettings() {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8" }
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

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a PDF", "Se exportó el reporte de perfiles a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReportePerfiles.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                throw;
            }
        }
    }
}
