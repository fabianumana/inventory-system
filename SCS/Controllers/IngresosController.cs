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
    public class IngresosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;
        private readonly AlmacenamientoService _almacenamientoService;

        public IngresosController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService, AlmacenamientoService almacenamientoService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
            _almacenamientoService = almacenamientoService;
        }

        [AutorizacionAccesoAtributo("IndexIngresos")]
        public async Task<IActionResult> Index(int? id, int? parteId, string parte)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var ingresos = dbContext.Ingresos
                .Where(i => i.Activo && i.Parte.Activo)
                .Include(i => i.Parte) 
                .AsQueryable();

            if (id.HasValue)
            {
                ingresos = ingresos.Where(i => i.Id_transaccion_ing == id);
            }

            if (parteId.HasValue)
            {
                ingresos = ingresos.Where(i => i.Id_parte == parteId);
            }

            if (!string.IsNullOrEmpty(parte))
            {
                ingresos = ingresos.Where(i => i.Parte.Nombre.Contains(parte, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new IngresosVM
            {
                Ingresos = await ingresos.ToListAsync(),
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
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Index", "Se accedió al listado de ingresos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }


        // GET: Ingresos/Details/5
        [AutorizacionAccesoAtributo("VerIngresos")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); 
            }

            using var dbContext = _contextFactory.CreateDbContext();

            var ingreso = await dbContext.Ingresos
                .Include(i => i.Parte)  
                .FirstOrDefaultAsync(m => m.Id_transaccion_ing == id);

            if (ingreso == null)
            {
                return NotFound(); 
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Ver Detalle Ingreso", $"Se visualizó el detalle del ingreso con ID {ingreso.Id_transaccion_ing}.", fechaAccion, horaAccion);
            }

            return View(ingreso); 
        }

        // GET: Ingresos/Create
        [HttpGet]
        [AutorizacionAccesoAtributo("CrearIngresos")]
        public IActionResult Create()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var partes = dbContext.Objetos.Where(o => o.Activo).ToList();
            if (!partes.Any())
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

            var ingreso = new Ingresos
            {
                Id_perfil = usuario.Id_perfiles,
                Usuario = usuario.Superior,
                Departamento = usuario.Departamento,
                Usuario_ingreso = usuario.User,
                Fecha_ingreso = DateTime.Now 
            };

            return View(ingreso);
        }

        // POST: Ingresos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("CrearIngresos")]
        public async Task<IActionResult> Create([Bind("Id_parte,Cantidad,Motivo,Fecha_ingreso")] Ingresos ingreso)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var usuario = dbContext.Perfiles.FirstOrDefault(u => u.User == User.Identity.Name);
                if (usuario == null)
                {
                    ModelState.AddModelError("", "No se pudo obtener el perfil del usuario autenticado.");
                    return View(ingreso);
                }

                var objeto = await dbContext.Objetos
                    .Include(o => o.Suplidor) 
                    .FirstOrDefaultAsync(o => o.Id_parte == ingreso.Id_parte && o.Activo);
                if (objeto == null)
                {
                    ModelState.AddModelError("", "El objeto seleccionado no está activo o no existe.");
                    return View(ingreso);
                }

                if (objeto.Suplidor == null || !objeto.Suplidor.Activo)
                {
                    ModelState.AddModelError("", "No se puede realizar el ingreso. El fabricante asociado a este objeto está deshabilitado.");
                    return View(ingreso);
                }

                if (objeto.Cantidad_Disponible + ingreso.Cantidad > objeto.Stock_Maximo)
                {
                    ModelState.AddModelError("", $"No se puede realizar el ingreso. El stock disponible excedería el Stock Máximo permitido ({objeto.Stock_Maximo}).");
                    return View(ingreso);
                }

                ingreso.Numero_serial = objeto.Numero_parte;
                ingreso.Id_perfil = usuario.Id_perfiles;
                ingreso.Usuario = usuario.Superior;
                ingreso.Departamento = usuario.Departamento;
                ingreso.Usuario_ingreso = usuario.User;

                dbContext.Ingresos.Add(ingreso);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await RecalcularCantidadDisponible(ingreso.Id_parte);

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    await _bitacorasService.RegistrarMovimientoAsync(
                        perfilId: userId,
                        usuario: User.Identity.Name,
                        tipoAccion: "Crear Ingreso",
                        descripcion: $"Se registró un ingreso para el objeto con ID {ingreso.Id_parte}.",
                        fechaAccion: fechaAccion,
                        horaAccion: horaAccion,
                        parteId: ingreso.Id_parte
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
                return View(ingreso);
            }
        }

        // GET: Ingresos/Edit/5
        [AutorizacionAccesoAtributo("EditarIngresos")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();

            var ingreso = await dbContext.Ingresos.FindAsync(id);
            if (ingreso == null)
            {
                return NotFound();
            }

            var partes = await dbContext.Objetos.Where(o => o.Activo).ToListAsync();
            if (!partes.Any())
            {
                TempData["ErrorMessage"] = "No hay partes activas disponibles.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Id_parte"] = new SelectList(partes, "Id_parte", "Nombre", ingreso.Id_parte);

            return View(ingreso);
        }

        // POST: Ingresos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EditarIngresos")]
        public async Task<IActionResult> Edit(int id, [Bind("Id_transaccion_ing,Id_parte,Cantidad,Motivo,Departamento,Numero_serial,Fecha_ingreso,Usuario,Usuario_ingreso")] Ingresos ingreso)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id != ingreso.Id_transaccion_ing)
            {
                return NotFound();
            }

            ModelState.Remove("Parte");

            var parteSeleccionada = await dbContext.Objetos.FirstOrDefaultAsync(o => o.Id_parte == ingreso.Id_parte && o.Activo);
            if (parteSeleccionada == null)
            {
                ModelState.AddModelError("Id_parte", "La parte seleccionada no es válida o no existe.");
            }
            else
            {
                ingreso.Numero_serial = parteSeleccionada.Numero_parte;
            }

            var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                ingreso.Id_perfil = perfilId;
            }
            else
            {
                ModelState.AddModelError("Id_perfil", "No se pudo determinar el perfil del usuario.");
            }

            if (!ModelState.IsValid)
            {
                var partes = await dbContext.Objetos.Where(o => o.Activo).ToListAsync();
                ViewData["Id_parte"] = new SelectList(partes, "Id_parte", "Nombre", ingreso.Id_parte);
                return View(ingreso);
            }

            try
            {
                dbContext.Update(ingreso);
                await dbContext.SaveChangesAsync();

                await RecalcularCantidadDisponible(ingreso.Id_parte);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IngresosExists(ingreso.Id_transaccion_ing))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: Ingresos/Delete/5
        [AutorizacionAccesoAtributo("EliminarIngresos")]
        public async Task<IActionResult> Disable(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            if (id == null)
            {
                return NotFound("ID no proporcionado.");
            }

            var ingreso = await dbContext.Ingresos
                .Include(i => i.Parte)
                .FirstOrDefaultAsync(m => m.Id_transaccion_ing == id);

            if (ingreso == null)
            {
                return NotFound($"El ingreso con ID {id} no existe.");
            }

            return View(ingreso); 
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

        // POST: Ingresos/Disable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarIngresos")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            // Depuración: Verificar el ID recibido
            Console.WriteLine($"ID recibido en DisableConfirmed: {id}");

            var ingreso = await dbContext.Ingresos
                .Include(i => i.Parte)
                .FirstOrDefaultAsync(i => i.Id_transaccion_ing == id);

            // Depuración: Verificar si el ingreso fue encontrado
            if (ingreso == null)
            {
                Console.WriteLine($"Ingreso con ID {id} no encontrado en la base de datos.");
                return NotFound($"El ingreso con ID {id} no existe.");
            }

            // Verificar si ya está deshabilitado
            if (!ingreso.Activo)
            {
                ModelState.AddModelError("", "El ingreso ya está deshabilitado.");
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Deshabilitar el ingreso
                ingreso.Activo = false;
                dbContext.Ingresos.Update(ingreso);

                // Actualizar la cantidad disponible del objeto relacionado
                if (ingreso.Parte != null)
                {
                    ingreso.Parte.Cantidad_Disponible -= ingreso.Cantidad;
                    dbContext.Objetos.Update(ingreso.Parte);
                }

                // Guardar cambios
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await RecalcularCantidadDisponible(ingreso.Id_parte);

                // Registrar movimiento en la bitácora
                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Deshabilitar Ingreso", $"Se deshabilitó el ingreso con ID {id}.", fechaAccion, horaAccion);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error al deshabilitar el ingreso con ID {id}: {ex.Message}");
                ModelState.AddModelError("", $"Ocurrió un error al deshabilitar el ingreso: {ex.Message}");
                return View(ingreso);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool IngresosExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Ingresos.Any(e => e.Id_transaccion_ing == id);
        }

        // Método para mostrar el reporte de ingresos
        [AutorizacionAccesoAtributo("ReporteIngresos")]
        public async Task<IActionResult> ReporteIngresos(int? id, int? parteId, string parte)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var ingresosQuery = dbContext.Ingresos
                .Include(i => i.Parte)
                .AsQueryable();

            if (id.HasValue)
            {
                ingresosQuery = ingresosQuery.Where(i => i.Id_transaccion_ing == id);
            }

            if (parteId.HasValue)
            {
                ingresosQuery = ingresosQuery.Where(i => i.Id_parte == parteId);
            }

            if (!string.IsNullOrEmpty(parte))
            {
                ingresosQuery = ingresosQuery.Where(i => i.Parte.Nombre.Contains(parte, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new IngresosVM
            {
                Ingresos = await ingresosQuery.ToListAsync(),
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

            var ingresos = await dbContext.Ingresos
                .Include(i => i.Parte)
                .Select(i => new Ingresos
                {
                    Id_transaccion_ing = i.Id_transaccion_ing,
                    Id_parte = i.Id_parte,
                    Parte = i.Parte,
                    Cantidad = i.Cantidad,
                    Usuario = i.Usuario,
                    Motivo = i.Motivo,
                    Departamento = i.Departamento,
                    Numero_serial = i.Numero_serial,
                    Fecha_ingreso = i.Fecha_ingreso,
                })
                .ToListAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Generar Reporte", "Se generó el reporte de ingresos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        // Método para exportar el reporte de ingresos a Excel
        [AutorizacionAccesoAtributo("ExcelIngresos")]
        public IActionResult ExportarExcelIngresos()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Ingresos.Include(i => i.Parte).ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReporteIngresos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "ID_Parte";
                worksheet.Cells[1, 3].Value = "Parte";
                worksheet.Cells[1, 4].Value = "Cantidad";
                worksheet.Cells[1, 5].Value = "Usuario";
                worksheet.Cells[1, 6].Value = "Departamento";
                worksheet.Cells[1, 7].Value = "Motivo";
                worksheet.Cells[1, 8].Value = "Fecha de Ingreso";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_transaccion_ing;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Parte?.Numero_parte;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Parte?.Nombre;
                    worksheet.Cells[i + 2, 3].Value = datos[i].Cantidad;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Usuario_ingreso;
                    worksheet.Cells[i + 2, 5].Value = datos[i].Departamento;
                    worksheet.Cells[i + 2, 6].Value = datos[i].Motivo;
                    worksheet.Cells[i + 2, 7].Value = datos[i].Fecha_ingreso?.ToShortDateString();
                }

                var stream = new MemoryStream(package.GetAsByteArray());

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a Excel", "Se exportó el reporte de ingresos a Excel.", fechaAccion, horaAccion);
                }

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteIngresos.xlsx");
            }
        }

        [AutorizacionAccesoAtributo("PdfIngresos")]
        public IActionResult ExportarPdfIngresos()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Ingresos.Include(i => i.Parte).ToList();

            var html = @"
            <h1>Reporte de Ingresos</h1>
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
                        <th>Fecha de Ingreso</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var ingreso in datos)
                    {
                        html += $@"
                <tr>
                    <td>{ingreso.Id_transaccion_ing}</td>
                    <td>{ingreso.Parte?.Numero_parte}</td>
                    <td>{ingreso.Parte?.Nombre}</td>
                    <td>{ingreso.Cantidad}</td>
                    <td>{ingreso.Usuario_ingreso}</td>
                    <td>{ingreso.Departamento}</td>
                    <td>{ingreso.Motivo}</td>
                    <td>{ingreso.Fecha_ingreso?.ToShortDateString()}</td>
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

                    _bitacorasService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a PDF", "Se exportó el reporte de ingresos a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReporteIngresos.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                throw;
            }
        }
    }
}
