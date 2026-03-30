using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class ObjetosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _movimientoService;
        private readonly FabricantesService _fabricantesService;

        public ObjetosController(IDbContextFactory<Service> contextFactory, BitacorasService movimientoService, FabricantesService fabricantesService)
        {
            _contextFactory = contextFactory;
            _movimientoService = movimientoService;
            _fabricantesService = fabricantesService;
        }

        // GET: Partes/Index/5
        [AutorizacionAccesoAtributo("IndexObjetos")]
        public async Task<IActionResult> Index(int? id, string numeroParte = "", bool? mostrarActivos = true)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var query = dbContext.Objetos.AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(o => o.Id_parte == id.Value);
            }

            if (!string.IsNullOrEmpty(numeroParte))
            {
                query = query.Where(o => o.Numero_parte.Contains(numeroParte));
            }

            if (mostrarActivos.HasValue)
            {
                query = query.Where(o => o.Activo == mostrarActivos.Value);
            }

            var objetos = await query
                .Select(o => new ObjetoDetalleVM
                {
                    Id_parte = o.Id_parte,
                    Numero_parte = o.Numero_parte,
                    Nombre = o.Nombre,
                    Descripcion = o.Descripcion,
                    Fabricante = o.Fabricante,
                    SuplidorId = o.SuplidorId,
                    Cantidad_Disponible = o.Cantidad_Disponible,
                    Ubicacion = o.Ubicacion,
                    Stock_Minimo = o.Stock_Minimo,
                    Stock_Maximo = o.Stock_Maximo,
                    Fecha_Ingreso = o.Fecha_Ingreso,
                    Fecha_Vencimiento = o.Fecha_Vencimiento,
                    Archivo = o.Archivo,
                    MimeType = o.MimeType,
                    Activo = o.Activo
                })
                .ToListAsync();

            var viewModel = new ObjetosVM
            {
                Objetos = objetos,
                FiltroId = id,
                FiltroParte = numeroParte,
                MostrarActivas = mostrarActivos
            };

            return View(viewModel);
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

        // GET: Partes/Details/5
        [AutorizacionAccesoAtributo("DetallesObjetos")]
        public async Task<IActionResult> Details(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var objeto = await dbContext.Objetos
                .Include(o => o.Suplidor)
                .FirstOrDefaultAsync(o => o.Id_parte == id && o.Activo);

            if (objeto == null)
            {
                return NotFound();
            }

            if (!objeto.Activo)
            {
                ViewBag.WarningMessage = "Este objeto está deshabilitado.";
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _movimientoService.RegistrarMovimientoAsync(
                    perfilId: userId,
                    usuario: User.Identity.Name,
                    tipoAccion: "Detalles Objeto",
                    descripcion: $"Se observaron los detlles del objeto con ID {id}.",
                    fechaAccion: fechaAccion,
                    horaAccion: horaAccion,
                    parteId: id
                );
            }

            return View(objeto);
        }

        //POST: Partes/Historial
        [AutorizacionAccesoAtributo("HistorialObjetos")]
        public async Task<IActionResult> Historial(int id)
        {
            Console.WriteLine($"ID de parte recibido: {id}");
            using var dbContext = _contextFactory.CreateDbContext();

            var historialMovimientos = await dbContext.Movimientos
                .Include(b => b.Ingresos)
                .Include(b => b.Egresos)
                .Include(b => b.EntradasSalidas)
                .Include(b => b.Perfil)
                .Where(b => b.ParteId_parte == id)
                .OrderBy(b => b.Fecha_accion) 
                .ToListAsync();

            if (!historialMovimientos.Any())
            {
                return NotFound("No se encontró historial para esta parte.");
            }

            return View(historialMovimientos);
        }

        // GET: Partes/Create
        [AutorizacionAccesoAtributo("CrearObjetos")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateObjetoViewModel
            {
                Suplidores = await _fabricantesService.GetFabricantesActivosAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("CrearObjetos")]
        public async Task<IActionResult> Create(CreateObjetoViewModel viewModel)
        {
            ModelState.Remove(nameof(viewModel.Suplidores));

            if (viewModel.SuplidorId == 0)
            {
                ModelState.AddModelError(nameof(viewModel.SuplidorId), "Debe seleccionar un fabricante válido.");
            }

            if (viewModel.Foto != null)
            {
                if (viewModel.Foto.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(viewModel.Foto), "El archivo no debe superar los 2 MB.");
                }

                var validMimeTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
                if (!validMimeTypes.Contains(viewModel.Foto.ContentType))
                {
                    ModelState.AddModelError(nameof(viewModel.Foto), "Solo se permiten imágenes en formato PNG, JPG o JPEG.");
                }
            }

            if (!ModelState.IsValid)
            {
                viewModel.Suplidores = await _fabricantesService.GetFabricantesActivosAsync();

                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => new
                    {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage)
                    });

                foreach (var error in errors)
                {
                    Console.WriteLine($"Field: {error.Field}");
                    foreach (var message in error.Errors)
                    {
                        Console.WriteLine($"Error: {message}");
                    }
                }

                return View(viewModel);
            }

            using var dbContext = _contextFactory.CreateDbContext();

            var fabricanteNombre = await dbContext.Fabricantes
                .Where(f => f.Id_suplidor == viewModel.SuplidorId)
                .Select(f => f.Nombre_Suplidor)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(fabricanteNombre))
            {
                ModelState.AddModelError(nameof(viewModel.SuplidorId), "Fabricante inválido.");
                viewModel.Suplidores = await _fabricantesService.GetFabricantesActivosAsync();
                return View(viewModel);
            }

            viewModel.Fabricante = fabricanteNombre;

            var objeto = new Objetos
            {
                Numero_parte = viewModel.Numero_parte,
                Nombre = viewModel.Nombre,
                Descripcion = viewModel.Descripcion,
                SuplidorId = viewModel.SuplidorId,
                Fabricante = fabricanteNombre,
                Cantidad_Disponible = viewModel.Cantidad_Disponible,
                Ubicacion = viewModel.Ubicacion,
                Stock_Minimo = viewModel.Stock_Minimo,
                Stock_Maximo = viewModel.Stock_Maximo,
                Fecha_Ingreso = viewModel.Fecha_Ingreso,
                Fecha_Vencimiento = viewModel.Fecha_Vencimiento
            };

            if (viewModel.Foto != null)
            {
                using var memoryStream = new MemoryStream();
                await viewModel.Foto.CopyToAsync(memoryStream);
                objeto.Archivo = memoryStream.ToArray();
                objeto.MimeType = viewModel.Foto.ContentType;
            }

            dbContext.Objetos.Add(objeto);
            await dbContext.SaveChangesAsync();

            await RecalcularCantidadDisponible(objeto.Id_parte);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _movimientoService.RegistrarMovimientoAsync(
                    perfilId: userId,
                    usuario: User.Identity.Name,
                    tipoAccion: "Crear Objeto",
                    descripcion: $"Se creó el objeto con ID {objeto.Id_parte}.",
                    fechaAccion: fechaAccion,
                    horaAccion: horaAccion,
                    parteId: objeto.Id_parte
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Partes/Edit/5
        [AutorizacionAccesoAtributo("EditarObjetos")]
        public async Task<IActionResult> Edit(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var objeto = await dbContext.Objetos.FindAsync(id);

            if (objeto == null)
            {
                return NotFound();
            }

            ViewBag.Suplidores = new SelectList(await dbContext.Fabricantes
                .Where(f => f.Activo)
                .ToListAsync(), "Id_suplidor", "Nombre_Suplidor");

            var viewModel = new ObjetoDetalleVM
            {
                Id_parte = objeto.Id_parte,
                Numero_parte = objeto.Numero_parte,
                Nombre = objeto.Nombre,
                Descripcion = objeto.Descripcion,
                Fabricante = objeto.Fabricante,
                SuplidorId = objeto.SuplidorId,
                Cantidad_Disponible = objeto.Cantidad_Disponible,
                Ubicacion = objeto.Ubicacion,
                Stock_Minimo = objeto.Stock_Minimo,
                Stock_Maximo = objeto.Stock_Maximo,
                Fecha_Ingreso = objeto.Fecha_Ingreso,
                Fecha_Vencimiento = objeto.Fecha_Vencimiento,
                Archivo = objeto.Archivo,
                MimeType = objeto.MimeType,
                Activo = objeto.Activo
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EditarObjetos")]
        public async Task<IActionResult> Edit(ObjetoDetalleVM viewModel)
        {
            if (!ModelState.IsValid)
            {
                using var dbContext = _contextFactory.CreateDbContext();
                ViewBag.Suplidores = new SelectList(await dbContext.Fabricantes
                    .Where(f => f.Activo)
                    .ToListAsync(), "Id_suplidor", "Nombre_Suplidor");

                return View(viewModel);
            }

            if (viewModel.Fecha_Vencimiento.HasValue && viewModel.Fecha_Vencimiento <= viewModel.Fecha_Ingreso)
            {
                ModelState.AddModelError("Fecha_Vencimiento", "La fecha de vencimiento debe ser posterior a la fecha de ingreso.");
                using var dbContext = _contextFactory.CreateDbContext();
                ViewBag.Suplidores = new SelectList(await dbContext.Fabricantes
                    .Where(f => f.Activo)
                    .ToListAsync(), "Id_suplidor", "Nombre_Suplidor");

                return View(viewModel);
            }

            using var dbContextContext = _contextFactory.CreateDbContext();

            var existeSuplidor = await dbContextContext.Fabricantes.AnyAsync(f =>
                f.Id_suplidor == viewModel.SuplidorId && f.Activo);

            if (!existeSuplidor)
            {
                ModelState.AddModelError("SuplidorId", "El suplidor seleccionado no es válido.");
                ViewBag.Suplidores = new SelectList(await dbContextContext.Fabricantes
                    .Where(f => f.Activo)
                    .ToListAsync(), "Id_suplidor", "Nombre_Suplidor");

                return View(viewModel);
            }

            var objeto = await dbContextContext.Objetos.FindAsync(viewModel.Id_parte);

            if (objeto == null)
            {
                return NotFound();
            }

            objeto.Numero_parte = viewModel.Numero_parte;
            objeto.Nombre = viewModel.Nombre;
            objeto.Descripcion = viewModel.Descripcion;
            objeto.Fabricante = viewModel.Fabricante;
            objeto.SuplidorId = viewModel.SuplidorId;
            objeto.Cantidad_Disponible = viewModel.Cantidad_Disponible;
            objeto.Ubicacion = viewModel.Ubicacion;
            objeto.Stock_Minimo = viewModel.Stock_Minimo;
            objeto.Stock_Maximo = viewModel.Stock_Maximo;
            objeto.Fecha_Ingreso = viewModel.Fecha_Ingreso;
            objeto.Fecha_Vencimiento = viewModel.Fecha_Vencimiento;

            await dbContextContext.SaveChangesAsync();

            await RecalcularCantidadDisponible(objeto.Id_parte);

            return RedirectToAction("Index");
        }

        [AutorizacionAccesoAtributo("DescargarObjetos")]
        public async Task<IActionResult> Descargar(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var objeto = await dbContext.Objetos.FindAsync(id);

            if (objeto == null || objeto.Archivo == null || string.IsNullOrEmpty(objeto.MimeType))
            {
                return NotFound();
            }

            var mimeMap = new Dictionary<string, string>
            {
                { "image/jpeg", ".jpg" },
                { "image/png", ".png" },
                { "application/pdf", ".pdf" },
                { "text/plain", ".txt" },
                { "application/msword", ".doc" },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" }
            };

            var extension = mimeMap.ContainsKey(objeto.MimeType) ? mimeMap[objeto.MimeType] : "";

            var fileName = $"{objeto.Numero_parte}{extension}";

            return File(objeto.Archivo, objeto.MimeType, fileName);
        }


        // GET: Partes/Delete/5
        [AutorizacionAccesoAtributo("EliminarObjetos")]
        public async Task<IActionResult> Disable(int? id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            if (id == null)
            {
                return NotFound();
            }

            var objeto = await dbContext.Objetos
                .Include(o => o.Ingresos)
                .Include(o => o.Egresos)
                .FirstOrDefaultAsync(o => o.Id_parte == id && o.Activo);

            if (objeto == null)
            {
                return NotFound();
            }

            if (objeto.Ingresos.Any() || objeto.Egresos.Any())
            {
                ViewBag.WarningMessage = "Este objeto tiene ingresos o egresos asociados. Si lo deshabilita, estas transacciones también serán deshabilitadas.";
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _movimientoService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Acceso al Formulario de Eliminación", $"Acceso al formulario de eliminación para el objeto con ID {id}.", fechaAccion, horaAccion);
            }

            return View(objeto);
        }

        // POST: Partes/Disable/5 
        [HttpPost]
        [AutorizacionAccesoAtributo("EliminarObjetos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var objeto = await dbContext.Objetos
                .Include(o => o.Ingresos)
                .Include(o => o.Egresos)
                .FirstOrDefaultAsync(o => o.Id_parte == id);

            if (objeto == null)
            {
                return NotFound();
            }

            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                objeto.Activo = false;
                dbContext.Objetos.Update(objeto);

                foreach (var ingreso in objeto.Ingresos)
                {
                    ingreso.Activo = false;
                    dbContext.Ingresos.Update(ingreso);
                }

                foreach (var egreso in objeto.Egresos)
                {
                    egreso.Activo = false;
                    dbContext.Egresos.Update(egreso);
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await RecalcularCantidadDisponible(objeto.Id_parte);

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    await _movimientoService.RegistrarMovimientoAsync(
                        perfilId: userId,
                        usuario: User.Identity.Name,
                        tipoAccion: "Deshabilitar Objeto",
                        descripcion: $"Se deshabilitó el objeto con ID {id}.",
                        fechaAccion: fechaAccion,
                        horaAccion: fechaAccion.TimeOfDay
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Ocurrió un error al deshabilitar el objeto: {ex.Message}");
                return View(objeto);
            }
        }

        private bool ObjetosExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Objetos.Any(e => e.Id_parte == id);
        }

        [AutorizacionAccesoAtributo("ReporteObjetos")]
        public async Task<IActionResult> ReporteObjetos(int? id, string numeroParte = "", bool? mostrarActivos = null)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var objetosQuery = dbContext.Objetos.AsQueryable();

            if (id.HasValue)
            {
                objetosQuery = objetosQuery.Where(o => o.Id_parte == id.Value);
            }

            if (!string.IsNullOrEmpty(numeroParte))
            {
                objetosQuery = objetosQuery.Where(o => o.Numero_parte.Contains(numeroParte));
            }

            if (mostrarActivos.HasValue && mostrarActivos.Value)
            {
                objetosQuery = objetosQuery.Where(o => o.Activo);
            }

            var objetos = await objetosQuery
                .Select(o => new ObjetoDetalleVM
                {
                    Id_parte = o.Id_parte,
                    Numero_parte = o.Numero_parte,
                    Nombre = o.Nombre,
                    Descripcion = o.Descripcion,
                    Fabricante = o.Fabricante,
                    SuplidorId = o.SuplidorId,
                    Cantidad_Disponible = o.Cantidad_Disponible,
                    Stock_Minimo = o.Stock_Minimo, 
                    Stock_Maximo = o.Stock_Maximo, 
                    Fecha_Ingreso = o.Fecha_Ingreso, 
                    Fecha_Vencimiento = o.Fecha_Vencimiento, 
                    Ubicacion = o.Ubicacion,
                    Archivo = o.Archivo,
                    MimeType = o.MimeType
                })
                .ToListAsync();

            var viewModel = new ObjetosVM
            {
                Objetos = objetos,
                FiltroId = id,
                FiltroParte = numeroParte,
                MostrarActivas = mostrarActivos
            };

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _movimientoService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Visualización Reporte Objetos", "Se visualizó el reporte de objetos.", fechaAccion, horaAccion);
            }

            return View(viewModel);
        }

        [AutorizacionAccesoAtributo("ExcelObjetos")]
        public IActionResult ExportarExcelObjetos()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Objetos.ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReporteObjetos");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Número de Parte";
                worksheet.Cells[1, 3].Value = "Nombre";
                worksheet.Cells[1, 4].Value = "Descripción";
                worksheet.Cells[1, 5].Value = "Fabricante";
                worksheet.Cells[1, 6].Value = "Cantidad Disponible";
                worksheet.Cells[1, 7].Value = "Ubicación";
                worksheet.Cells[1, 8].Value = "Suplidor";
                worksheet.Cells[1, 9].Value = "Fecha Ingreso";
                worksheet.Cells[1, 10].Value = "Fecha Vencimiento";

                for (int i = 0; i < datos.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = datos[i].Id_parte;
                    worksheet.Cells[i + 2, 2].Value = datos[i].Numero_parte;
                    worksheet.Cells[i + 2, 3].Value = datos[i].Nombre;
                    worksheet.Cells[i + 2, 4].Value = datos[i].Descripcion;
                    worksheet.Cells[i + 2, 5].Value = datos[i].Fabricante;
                    worksheet.Cells[i + 2, 6].Value = datos[i].Cantidad_Disponible;
                    worksheet.Cells[i + 2, 7].Value = datos[i].Ubicacion;
                    worksheet.Cells[i + 2, 8].Value = datos[i].Suplidor?.Nombre_Suplidor;
                    worksheet.Cells[i + 2, 9].Value = datos[i].Fecha_Ingreso.ToShortDateString();
                    worksheet.Cells[i + 2, 10].Value = datos[i].Fecha_Vencimiento?.ToShortDateString();
                }

                var stream = new MemoryStream(package.GetAsByteArray());

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    _movimientoService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar Excel", "Se exportó el reporte de objetos a Excel.", fechaAccion, horaAccion);
                }

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteObjetos.xlsx");
            }
        }

        [AutorizacionAccesoAtributo("PdfObjetos")]
        public IActionResult ExportarPdfObjetos()
        {
            var libPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "libwkhtmltox.dll");

            if (!System.IO.File.Exists(libPath))
            {
                throw new FileNotFoundException($"No se encontró la biblioteca libwkhtmltox en la ruta {libPath}");
            }

            var context = new CustomAssemblyLoadContext();
            context.LoadUnmanagedLibrary(libPath);

            using var dbContext = _contextFactory.CreateDbContext();

            var datos = dbContext.Objetos.ToList();

            var html = @"
    <h1>Reporte de Objetos</h1>
    <table border='1' cellpadding='5' cellspacing='0' width='100%'>
        <thead>
            <tr>
                <th>ID</th>
                <th>Número de Parte</th>
                <th>Parte</th>
                <th>Descripción</th>
                <th>Fabricante</th>
                <th>Cantidad Disponible</th>
                <th>Ubicación</th>
                <th>Suplidor</th>
                <th>Fecha Ingreso</th>
                <th>Fecha Vencimiento</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in datos)
            {
                html += $@"
        <tr>
            <td>{item.Id_parte}</td>
            <td>{item.Numero_parte}</td>
            <td>{item.Nombre}</td>
            <td>{item.Descripcion}</td>
            <td>{item.Fabricante}</td>
            <td>{item.Cantidad_Disponible}</td>
            <td>{item.Ubicacion}</td>
            <td>{item.Suplidor?.Nombre_Suplidor}</td>
            <td>{item.Fecha_Ingreso.ToShortDateString()}</td>
            <td>{item.Fecha_Vencimiento?.ToShortDateString()}</td>
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
            new ObjectSettings()
            {
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
                    _movimientoService.RegistrarMovimientoAsync(userId, User.Identity.Name, "Exportar PDF", "Se exportó el reporte de objetos a PDF.", fechaAccion, horaAccion);
                }

                return File(pdf, "application/pdf", "ReporteObjetos.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el PDF: {ex.Message}");
                throw;
            }
        }
    }
}
