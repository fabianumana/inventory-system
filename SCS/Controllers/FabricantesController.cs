using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
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
    public class FabricantesController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _movimientoService;

        public FabricantesController(IDbContextFactory<Service> contextFactory, BitacorasService movimientoService)
        {
            _contextFactory = contextFactory;
            _movimientoService = movimientoService;
        }

        // GET: Fabricantes/Index
        [AutorizacionAccesoAtributo("IndexFabricantes")]
        public async Task<IActionResult> Index(int? id, int? parteId, string nombreSuplidor, bool? mostrarActivos = null)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var fabricantes = dbContext.Fabricantes.Where(f => f.Activo).AsQueryable();

            if (id.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Id_suplidor == id);
            }

            if (parteId.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Partes.Any(p => p.Id_parte == parteId));
            }

            if (!string.IsNullOrEmpty(nombreSuplidor))
            {
                fabricantes = fabricantes.Where(f => f.Nombre_Suplidor.Contains(nombreSuplidor));
            }

            if (mostrarActivos.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Activo == mostrarActivos.Value);
            }

            var viewModel = new FabricantesVM
            {
                Fabricantes = await fabricantes.ToListAsync(),
                FiltroId = id,
                FiltroParteId = parteId,
                FiltroSuplidor = nombreSuplidor,
                MostrarActivos = mostrarActivos
            };

            return View(viewModel);
        }

        // GET: Fabricantes/Details/5
        [AutorizacionAccesoAtributo("DetallesFabricantes")]
        public async Task<IActionResult> Details(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var fabricante = await dbContext.Fabricantes
                .Include(f => f.Partes)
                .FirstOrDefaultAsync(m => m.Id_suplidor == id);

            if (fabricante == null)
            {
                return NotFound();
            }

            var usuarioId = User.Identity.Name;
            var perfil = await dbContext.Perfiles.FirstOrDefaultAsync(p => p.User == usuarioId);

            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var movimiento = new BitacoraMovimientos
                {
                    Id_perfi = perfil.Id_perfiles,
                    Tipo_accion = "Visualizar",
                    Descripcion = $"Se visualizó el fabricante con ID {fabricante.Id_suplidor}.",
                    Fecha_accion = DateTime.Now
                };

                dbContext.Movimientos.Add(movimiento);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
            }

            return View(fabricante);
        }

        // GET: Fabricantes/Create
        [AutorizacionAccesoAtributo("CrearFabricantes")]
        public IActionResult Create()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var usuarioId = User.Identity.Name;
            var perfil = dbContext.Perfiles.FirstOrDefault(p => p.User == usuarioId);
            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            var movimiento = new BitacoraMovimientos
            {
                Id_perfi = perfil.Id_perfiles,
                Tipo_accion = "Acceso al Formulario de Creación",
                Descripcion = "El usuario accedió al formulario de creación de un nuevo fabricante.",
                Fecha_accion = DateTime.Now
            };

            dbContext.Movimientos.Add(movimiento);
            dbContext.SaveChanges();

            return View();
        }

        // POST: Fabricantes/Create
        [AutorizacionAccesoAtributo("CrearFabricantes")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id_suplidor,Nombre_Suplidor,Contacto,Telefono,Correo,Direccion,Pais,Condiciones,Colateral,Fecha_Registro")] Fabricantes fabricante)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                return View(fabricante);
            }

            using var transaction = await dbContext.Database.BeginTransactionAsync();

            dbContext.Add(fabricante);
            await dbContext.SaveChangesAsync();

            var usuarioId = User.Identity.Name;
            var perfil = dbContext.Perfiles.FirstOrDefault(p => p.User == usuarioId);
            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            var movimiento = new BitacoraMovimientos
            {
                Id_perfi = perfil.Id_perfiles,
                Tipo_accion = "Crear",
                Descripcion = $"Se creó un nuevo fabricante con ID {fabricante.Id_suplidor}.",
                Fecha_accion = DateTime.Now
            };

            dbContext.Movimientos.Add(movimiento);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Fabricantes/Edit/5
        [AutorizacionAccesoAtributo("EditarFabricantes")]
        public async Task<IActionResult> Edit(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var fabricante = await dbContext.Fabricantes.FindAsync(id);
            if (fabricante == null)
            {
                return NotFound();
            }

            var usuarioId = User.Identity.Name;
            var perfil = dbContext.Perfiles.FirstOrDefault(p => p.User == usuarioId);
            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            var movimiento = new BitacoraMovimientos
            {
                Id_perfi = perfil.Id_perfiles,
                Tipo_accion = "Acceso al Formulario de Edición",
                Descripcion = $"El usuario accedió al formulario de edición para el fabricante con ID {id}.",
                Fecha_accion = DateTime.Now
            };

            dbContext.Movimientos.Add(movimiento);
            await dbContext.SaveChangesAsync();

            return View(fabricante);
        }

        // POST: Fabricantes/Edit/5
        [AutorizacionAccesoAtributo("EditarFabricantes")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_suplidor,Nombre_Suplidor,Contacto,Telefono,Correo,Direccion,Pais,Condiciones,Colateral,Fecha_Registro")] Fabricantes fabricante)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var existingFabricante = await dbContext.Fabricantes.FindAsync(id);
            if (existingFabricante == null)
            {
                return NotFound();
            }

            if (id != fabricante.Id_suplidor)
            {
                return BadRequest("El ID proporcionado no coincide con el ID del fabricante.");
            }

            if (await dbContext.Fabricantes.AnyAsync(f => f.Correo == fabricante.Correo && f.Id_suplidor != id))
            {
                ModelState.AddModelError(nameof(fabricante.Correo), "El correo ya está en uso por otro fabricante.");
            }

            if (await dbContext.Fabricantes.AnyAsync(f => f.Telefono == fabricante.Telefono && f.Id_suplidor != id))
            {
                ModelState.AddModelError(nameof(fabricante.Telefono), "El teléfono ya está en uso por otro fabricante.");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    existingFabricante.Nombre_Suplidor = fabricante.Nombre_Suplidor;
                    existingFabricante.Contacto = fabricante.Contacto;
                    existingFabricante.Telefono = fabricante.Telefono;
                    existingFabricante.Correo = fabricante.Correo;
                    existingFabricante.Direccion = fabricante.Direccion;
                    existingFabricante.Pais = fabricante.Pais;
                    existingFabricante.Condiciones = fabricante.Condiciones;
                    existingFabricante.Colateral = fabricante.Colateral;
                    existingFabricante.Fecha_Registro = fabricante.Fecha_Registro;

                    dbContext.Update(existingFabricante);
                    await dbContext.SaveChangesAsync();

                    var usuarioId = User.Identity.Name;
                    var perfil = dbContext.Perfiles.FirstOrDefault(p => p.User == usuarioId);
                    if (perfil == null)
                    {
                        return NotFound("Perfil no encontrado.");
                    }

                    var movimiento = new BitacoraMovimientos
                    {
                        Id_perfi = perfil.Id_perfiles,
                        Tipo_accion = "Editar",
                        Descripcion = $"Se actualizó el fabricante con ID {fabricante.Id_suplidor}.",
                        Fecha_accion = DateTime.Now
                    };

                    dbContext.Movimientos.Add(movimiento);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!FabricantesExists(fabricante.Id_suplidor))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fabricante);
        }

        // GET: Fabricantes/Disable/5
        [AutorizacionAccesoAtributo("EliminarFabricantes")]
        public async Task<IActionResult> Disable(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var fabricante = await dbContext.Fabricantes
                .Include(f => f.Partes.Where(p => p.Activo))
                .FirstOrDefaultAsync(f => f.Id_suplidor == id);

            if (fabricante == null)
            {
                return NotFound();
            }

            if (fabricante.Partes.Any(p => p.Activo))
            {
                ViewBag.Warning = $"Este fabricante tiene {fabricante.Partes.Count(p => p.Activo)} objeto(s) activo(s). Debe deshabilitar o reasignar estos objetos antes de continuar.";
            }

            var usuarioId = User.Identity.Name;
            var perfil = dbContext.Perfiles.FirstOrDefault(p => p.User == usuarioId);
            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            var movimiento = new BitacoraMovimientos
            {
                Id_perfi = perfil.Id_perfiles,
                Tipo_accion = "Acceso al Formulario de Deshabilitación",
                Descripcion = $"El usuario accedió al formulario de deshabilitación para el fabricante con ID {id}.",
                Fecha_accion = DateTime.Now
            };

            dbContext.Movimientos.Add(movimiento);
            await dbContext.SaveChangesAsync();

            return View(fabricante);
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

        // POST: Fabricantes/Disable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarFabricantes")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id <= 0)
            {
                return BadRequest("ID inválido.");
            }

            var fabricante = await dbContext.Fabricantes
                .Include(f => f.Partes.Where(p => p.Activo))
                .FirstOrDefaultAsync(f => f.Id_suplidor == id);

            if (fabricante == null)
            {
                return NotFound($"Fabricante con ID {id} no encontrado.");
            }

            var usuarioId = User.Identity.Name;
            var perfil = await dbContext.Perfiles.FirstOrDefaultAsync(p => p.User == usuarioId);

            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            if (fabricante.Partes.Any(p => p.Activo))
            {
                foreach (var parte in fabricante.Partes)
                {
                    parte.Activo = false;

                    var movimientoParte = new BitacoraMovimientos
                    {
                        Id_perfi = perfil.Id_perfiles, 
                        Tipo_accion = "Deshabilitar",
                        Descripcion = $"La parte con ID {parte.Id_parte} fue deshabilitada automáticamente al deshabilitar el fabricante con ID {fabricante.Id_suplidor}.",
                        Fecha_accion = DateTime.Now
                    };
                    dbContext.Movimientos.Add(movimientoParte);

                    await RecalcularCantidadDisponible(parte.Id_parte);
                }
            }

            fabricante.Activo = false;

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                dbContext.Fabricantes.Update(fabricante);
                await dbContext.SaveChangesAsync();

                var movimiento = new BitacoraMovimientos
                {
                    Id_perfi = perfil.Id_perfiles, 
                    Tipo_accion = "Deshabilitar",
                    Descripcion = $"Se deshabilitó el fabricante con ID {fabricante.Id_suplidor}.",
                    Fecha_accion = DateTime.Now
                };

                dbContext.Movimientos.Add(movimiento);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error inesperado: {ex.Message}");
                return View(fabricante);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FabricantesExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Fabricantes.Any(e => e.Id_suplidor == id);
        }

        [AutorizacionAccesoAtributo("ReporteFabricantes")]
        public async Task<IActionResult> ReporteFabricantes(int? id, int? parteId, string nombreSuplidor, bool? mostrarActivos = null)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var fabricantes = dbContext.Fabricantes.AsQueryable();

            if (id.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Id_suplidor == id);
            }

            if (parteId.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Partes.Any(p => p.Id_parte == parteId));
            }

            if (!string.IsNullOrEmpty(nombreSuplidor))
            {
                fabricantes = fabricantes.Where(f => f.Nombre_Suplidor.Contains(nombreSuplidor));
            }

            if (mostrarActivos.HasValue)
            {
                fabricantes = fabricantes.Where(f => f.Activo == mostrarActivos.Value);
            }

            var viewModel = new FabricantesVM
            {
                Fabricantes = await fabricantes.ToListAsync(),
                FiltroId = id,
                FiltroParteId = parteId,
                FiltroSuplidor = nombreSuplidor,
                MostrarActivos = mostrarActivos
            };

            return View(viewModel);
        }

        //Metodo para Exportar PDF
        [AutorizacionAccesoAtributo("PdfFabricantes")]
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

            var datos = dbContext.Fabricantes.ToList();

            var html = @"
            <h1>Reporte de Fabricantes</h1>
            <table border='1' cellpadding='5' cellspacing='0' width='100%'>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Nombre del Suplidor</th>
                        <th>Contacto</th>
                        <th>Teléfono</th>
                        <th>Correo</th>
                        <th>País</th>
                        <th>Condiciones</th>
                        <th>Colateral</th>
                    </tr>
                </thead>
                <tbody>";

                    foreach (var item in datos)
                    {
                        html += $@"
                <tr>
                    <td>{item.Id_suplidor}</td>
                    <td>{item.Nombre_Suplidor}</td>
                    <td>{item.Contacto}</td>
                    <td>{item.Telefono}</td>
                    <td>{item.Correo}</td>
                    <td>{item.Pais}</td>
                    <td>{item.Condiciones}</td>
                    <td>{item.Colateral}</td>
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

                    _movimientoService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar a PDF", "Se exportó el reporte de fabricantes a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReporteFabricantes.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                throw;
            }
        }

        //Metodo para Exportar Excel
        [AutorizacionAccesoAtributo("ExcelFabricantes")]
        public IActionResult ExportarExcel()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Fabricantes.ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nombre Suplidor";
                worksheet.Cells[1, 3].Value = "Contacto";
                worksheet.Cells[1, 4].Value = "Teléfono";
                worksheet.Cells[1, 5].Value = "Correo";
                worksheet.Cells[1, 6].Value = "Pais";
                worksheet.Cells[1, 7].Value = "Condiciones";
                worksheet.Cells[1, 8].Value = "Colateral";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_suplidor;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Nombre_Suplidor;
                    worksheet.Cells[i + 2, 3].Value = datos[i].Contacto;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Telefono;
                    worksheet.Cells[i + 2, 5].Value = datos[i].Correo;
                    worksheet.Cells[i + 2, 6].Value = datos[i].Pais;
                    worksheet.Cells[i + 2, 7].Value = datos[i].Condiciones;
                    worksheet.Cells[i + 2, 8].Value = datos[i].Colateral;
                }

                var stream = new MemoryStream(package.GetAsByteArray());

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteFabricantes.xlsx");
            }
        }
    }
}
