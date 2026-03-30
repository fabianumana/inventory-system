using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SCS.Autorizacion;
using SCS.Helpers;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class EgresosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;
        private readonly AlmacenamientoService _almacenamientoService;

        public EgresosController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService, AlmacenamientoService almacenamientoService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
            _almacenamientoService = almacenamientoService;
        }

        [AutorizacionAccesoAtributo("IndexEgresos")]
        public async Task<IActionResult> Index(int? id, int? parteId, string parte)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var egresos = dbContext.Egresos
                .Where(e => e.Activo && e.Parte.Activo)
                .Include(e => e.Parte) 
                .AsQueryable();

            if (id.HasValue)
            {
                egresos = egresos.Where(e => e.Id_transaccion_eg == id);
            }

            if (parteId.HasValue)
            {
                egresos = egresos.Where(e => e.Id_parte == parteId);
            }

            if (!string.IsNullOrEmpty(parte))
            {
                egresos = egresos.Where(e => e.Parte.Nombre.Contains(parte, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new EgresosVM
            {
                Egresos = await egresos.ToListAsync(),
                Partes = dbContext.Objetos
                    .Where(o => o.Activo)
                    .Select(o => new SelectListItem
                    {
                        Value = o.Id_parte.ToString(),
                        Text = $"{o.Nombre} - {o.Numero_parte}"
                    }).ToList(),
                FiltroId = id,
                FiltroParteId = parteId,
                FiltroParte = parte
            };

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Index", "Se accedió al listado de egresos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        // GET: Egresos/Details/5
        [AutorizacionAccesoAtributo("DetallesEgresos")]
        public async Task<IActionResult> Details(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var egreso = await dbContext.Egresos
                .Include(e => e.Parte) 
                .FirstOrDefaultAsync(m => m.Id_transaccion_eg == id);

            if (egreso == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Detalles Egreso", $"El usuario visualizó los detalles del egreso con ID {id}.", fechaAccion, horaAccion);
            }

            return View(egreso);
        }

        // GET: Egresos/Create
        [HttpGet]
        [AutorizacionAccesoAtributo("CrearEgresos")]
        public IActionResult Create()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var existenObjetos = dbContext.Objetos.Any(o => o.Activo);
            if (!existenObjetos)
            {
                TempData["ErrorMessage"] = "No hay objetos activos en el sistema. Por favor, registre objetos antes de proceder.";
                return RedirectToAction("Index");
            }

            var usuario = dbContext.Perfiles.FirstOrDefault(u => u.User == User.Identity.Name);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "No se pudo obtener el perfil del usuario autenticado.";
                return RedirectToAction("Index");
            }

            ViewBag.Objetos = dbContext.Objetos.Where(o => o.Activo)
                .Select(o => new
                {
                    Id_parte = o.Id_parte,
                    Text = $"{o.Nombre} - {o.Numero_parte} (Disponible: {o.Cantidad_Disponible})",
                    NumeroParte = o.Numero_parte
                }).ToList();

            var egreso = new Egresos
            {
                Id_perfil = usuario.Id_perfiles,
                Usuario = usuario.Superior,
                Departamento = usuario.Departamento,
                Usuario_salida = usuario.User,
                Fecha_salida = DateTime.Now
            };

            return View(egreso);
        }

        // POST: Egresos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("CrearEgresos")]
        public async Task<IActionResult> Create([Bind("Id_parte,Cantidad,Motivo,Fecha_salida")] Egresos egreso)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var usuario = dbContext.Perfiles.FirstOrDefault(u => u.User == User.Identity.Name);
                if (usuario == null)
                {
                    ModelState.AddModelError("", "No se pudo obtener el perfil del usuario autenticado.");
                    return View(egreso);
                }

                var objeto = await dbContext.Objetos
                    .Include(o => o.Suplidor) 
                    .FirstOrDefaultAsync(o => o.Id_parte == egreso.Id_parte && o.Activo);
                if (objeto == null)
                {
                    ModelState.AddModelError("", "El objeto seleccionado no está activo o no existe.");
                    return View(egreso);
                }

                if (objeto.Suplidor == null || !objeto.Suplidor.Activo)
                {
                    ModelState.AddModelError("", "No se puede realizar el egreso. El fabricante asociado a este objeto está deshabilitado.");
                    return View(egreso);
                }

                if (objeto.Cantidad_Disponible - egreso.Cantidad < objeto.Stock_Minimo)
                {
                    ModelState.AddModelError("", $"No se puede realizar el egreso. El stock disponible quedaría por debajo del Stock Mínimo permitido ({objeto.Stock_Minimo}).");
                    return View(egreso);
                }

                egreso.Numero_serial = objeto.Numero_parte;
                egreso.Id_perfil = usuario.Id_perfiles;
                egreso.Usuario = usuario.Superior;
                egreso.Departamento = usuario.Departamento;
                egreso.Usuario_salida = usuario.User;
                egreso.Fecha_salida = DateTime.Now;

                objeto.Cantidad_Disponible -= egreso.Cantidad;
                dbContext.Objetos.Update(objeto);

                dbContext.Egresos.Add(egreso);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await RecalcularCantidadDisponible(egreso.Id_parte);

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    await _bitacorasService.RegistrarMovimientoAsync(
                        perfilId: userId,
                        usuario: User.Identity.Name,
                        tipoAccion: "Crear Egreso",
                        descripcion: $"Se registró un egreso para el objeto con ID {egreso.Id_parte}.",
                        fechaAccion: fechaAccion,
                        horaAccion: horaAccion,
                        parteId: egreso.Id_parte
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Ocurrió un error al registrar el egreso: {ex.Message}");
                return View(egreso);
            }
        }

        // GET: Egresos/Edit/5
        [AutorizacionAccesoAtributo("EditarEgresos")]
        public async Task<IActionResult> Edit(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var egreso = await dbContext.Egresos.FindAsync(id);
            if (egreso == null)
            {
                return NotFound();
            }

            var partes = await dbContext.Objetos.Where(o => o.Activo).ToListAsync();
            ViewData["Id_parte"] = new SelectList(partes, "Id_parte", "Nombre", egreso.Id_parte);

            return View(egreso);
        }


        // POST: Egresos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EditarEgresos")]
        public async Task<IActionResult> Edit(int id, [Bind("Id_transaccion_eg,Id_parte,Cantidad,Motivo,Departamento,Numero_serial,Fecha_salida,Usuario,Usuario_salida")] Egresos egreso)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id != egreso.Id_transaccion_eg)
            {
                return NotFound();
            }

            ModelState.Remove("Parte");

            var parteSeleccionada = await dbContext.Objetos.FirstOrDefaultAsync(o => o.Id_parte == egreso.Id_parte && o.Activo);
            if (parteSeleccionada == null)
            {
                ModelState.AddModelError("Id_parte", "La parte seleccionada no es válida o no existe.");
            }
            else
            {
                egreso.Numero_serial = parteSeleccionada.Numero_parte;
            }

            var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                egreso.Id_perfil = perfilId;
            }
            else
            {
                ModelState.AddModelError("Id_perfil", "No se pudo determinar el perfil del usuario.");
            }

            if (!ModelState.IsValid)
            {
                var partes = await dbContext.Objetos.Where(o => o.Activo).ToListAsync();
                ViewData["Id_parte"] = new SelectList(partes, "Id_parte", "Nombre", egreso.Id_parte);
                return View(egreso);
            }

            try
            {
                dbContext.Update(egreso);
                await dbContext.SaveChangesAsync();

                await RecalcularCantidadDisponible(egreso.Id_parte);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EgresosExists(egreso.Id_transaccion_eg))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: Egresos/Disable/5
        [AutorizacionAccesoAtributo("EliminarEgresos")]
        public async Task<IActionResult> Disable(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var egreso = await dbContext.Egresos
                .Include(e => e.Parte)
                .FirstOrDefaultAsync(m => m.Id_transaccion_eg == id);

            if (egreso == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Deshabilitar", $"Se accedió a la deshabilitación del egreso con ID {id}.", fechaAccion, horaAccion);
            }

            return View(egreso);
        }

        private async Task RecalcularCantidadDisponible(int parteId)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            // Obtiene el objeto con la cantidad inicial
            var objeto = await dbContext.Objetos.FirstOrDefaultAsync(o => o.Id_parte == parteId);

            if (objeto != null && objeto.Activo)
            {
                // Recalcula la cantidad disponible basado en la cantidad inicial
                objeto.Cantidad_Disponible = (
                    objeto.Cantidad_Disponible // Cantidad inicial asignada al crear el objeto
                    + (await dbContext.Ingresos
                        .Where(i => i.Id_parte == parteId && i.Activo)
                        .SumAsync(i => (int?)i.Cantidad) ?? 0) // Suma de ingresos activos
                    - (await dbContext.Egresos
                        .Where(e => e.Id_parte == parteId && e.Activo)
                        .SumAsync(e => (int?)e.Cantidad) ?? 0) // Resta de egresos activos
                );

                dbContext.Objetos.Update(objeto);
                await dbContext.SaveChangesAsync();
            }
        }

        // POST: Egresos/Disable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarEgresos")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var egreso = await dbContext.Egresos.Include(e => e.Parte).FirstOrDefaultAsync(e => e.Id_transaccion_eg == id);

            if (egreso != null && egreso.Activo)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    egreso.Activo = false;
                    dbContext.Egresos.Update(egreso);

                    if (egreso.Parte != null)
                    {
                        egreso.Parte.Cantidad_Disponible += egreso.Cantidad;
                        dbContext.Objetos.Update(egreso.Parte);
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await RecalcularCantidadDisponible(egreso.Id_parte);

                    var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(perfilIdClaim, out int perfilId))
                    {
                        DateTime fechaAccion = DateTime.Now;
                        TimeSpan horaAccion = fechaAccion.TimeOfDay;
                        await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Deshabilitar Egreso", $"Se deshabilitó el egreso con ID {id}.", fechaAccion, horaAccion);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al deshabilitar el egreso.");
                    return View(egreso);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EgresosExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Egresos.Any(e => e.Id_transaccion_eg == id);
        }

        // Método para mostrar el reporte de egresos
        [AutorizacionAccesoAtributo("ReporteEgresos")]
        public async Task<IActionResult> ReporteEgresos(int? id, int? parteId, string parte)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var egresosQuery = dbContext.Egresos
                .Include(e => e.Parte)
                .AsQueryable();

            if (id.HasValue)
            {
                egresosQuery = egresosQuery.Where(e => e.Id_transaccion_eg == id);
            }

            if (parteId.HasValue)
            {
                egresosQuery = egresosQuery.Where(e => e.Id_parte == parteId);
            }

            if (!string.IsNullOrEmpty(parte))
            {
                egresosQuery = egresosQuery.Where(e => e.Parte.Nombre.Contains(parte, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new EgresosVM
            {
                Egresos = await egresosQuery.ToListAsync(),
                Partes = dbContext.Objetos
                    .Where(o => o.Activo)
                    .Select(o => new SelectListItem
            {
                Value = o.Id_parte.ToString(),
                Text = $"{o.Nombre} - {o.Numero_parte}"
            }).ToList(),
                FiltroId = id,
                FiltroParteId = parteId,
                FiltroParte = parte
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Generar Reporte", "Se generó el reporte de egresos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        // Método para exportar el reporte de egresos a Excel
        [AutorizacionAccesoAtributo("ExcelEgresos")]
        public IActionResult ExportarExcelEgresos()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Egresos.Include(e => e.Parte).ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReporteEgresos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "ID_Parte";
                worksheet.Cells[1, 3].Value = "Parte";
                worksheet.Cells[1, 4].Value = "Cantidad";
                worksheet.Cells[1, 5].Value = "Usuario";
                worksheet.Cells[1, 6].Value = "Departamento";
                worksheet.Cells[1, 7].Value = "Motivo";
                worksheet.Cells[1, 8].Value = "Fecha de Salida";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_transaccion_eg;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Parte?.Numero_parte;
                    worksheet.Cells[i + 2, 3].Value = datos[i].Parte?.Nombre;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Cantidad;
                    worksheet.Cells[i + 2, 5].Value = datos[i].Usuario_salida;
                    worksheet.Cells[i + 2, 6].Value = datos[i].Departamento;
                    worksheet.Cells[i + 2, 7].Value = datos[i].Motivo;
                    worksheet.Cells[i + 2, 8].Value = datos[i].Fecha_salida?.ToShortDateString();
                }

                var stream = new MemoryStream(package.GetAsByteArray());

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a Excel", "Se exportó el reporte de egresos a Excel.", fechaAccion, horaAccion);
                }

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteEgresos.xlsx");
            }
        }

        // Método para exportar el reporte de egresos a PDF
        [AutorizacionAccesoAtributo("PdfEgresos")]
        public IActionResult ExportarPdfEgresos()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Egresos.Include(e => e.Parte).ToList();

            var html = @"
            <h1>Reporte de Egresos</h1>
            <table border='1' cellpadding='5' cellspacing='0' width='100%'>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>ID_Parte</th>
                        <th>Parte</th>
                        <th>Cantidad</th>
                        <th>Usuario</th>
                        <th>Departamento</th>
                        <th>Motivo</th>
                        <th>Fecha de Salida</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var egreso in datos)
                    {
                        html += $@"
                <tr>
                    <td>{egreso.Id_transaccion_eg}</td>
                    <td>{egreso.Parte?.Numero_parte}</td>
                    <td>{egreso.Parte?.Nombre}</td>
                    <td>{egreso.Cantidad}</td>
                    <td>{egreso.Usuario_salida}</td>
                    <td>{egreso.Departamento}</td>
                    <td>{egreso.Motivo}</td>
                    <td>{egreso.Fecha_salida?.ToShortDateString()}</td>
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

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a PDF", "Se exportó el reporte de egresos a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReporteEgresos.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                throw;
            }
        }
    }
}
