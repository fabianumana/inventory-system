using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SCS.Autorizacion;
using SCS.Models;
using SCS.Services;
using SCS.Helpers;

namespace SCS.Controllers
{
    public class AlmacenamientoController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;

        public AlmacenamientoController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
        }

        // GET: Almacenamiento
        [AutorizacionAccesoAtributo("IndexAlmacenamiento")]
        public async Task<IActionResult> Index(string parte, string suplidor)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var query = from o in dbContext.Objetos
                        join f in dbContext.Fabricantes on o.SuplidorId equals f.Id_suplidor
                        where o.Activo && f.Activo
                        select new Almacenamiento
                        {
                            Id_parte = o.Id_parte,
                            Part = o.Nombre,
                            Numero_parte = o.Numero_parte,
                            Descripcion = o.Descripcion,
                            Cantidad_Disponible = o.Cantidad_Disponible,
                            Ubicacion = o.Ubicacion,
                            Fecha_Ingreso = o.Fecha_Ingreso,
                            Fecha_Vencimiento = o.Fecha_Vencimiento,
                            Suplidor = f.Nombre_Suplidor,
                            Colateral = f.Colateral,
                            Archivo = o.Archivo,  
                            MimeType = o.MimeType,
                            Activo = o.Activo
                        };

            if (!string.IsNullOrEmpty(parte))
            {
                query = query.Where(q => q.Part.ToLower().Contains(parte.ToLower()));
            }

            if (!string.IsNullOrEmpty(suplidor))
            {
                query = query.Where(q => q.Suplidor.ToLower().Contains(suplidor.ToLower()));
            }

            var almacenamiento = await query.ToListAsync();

            ViewBag.FiltroParte = parte;
            ViewBag.FiltroSuplidor = suplidor;

            var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Almacenamiento", "Se accedió al listado de almacenamiento.", fechaAccion, fechaAccion.TimeOfDay);
            }

            return View(almacenamiento);
        }

        // GET: Almacenamiento/Details/5
        [AutorizacionAccesoAtributo("DetallesAlmacenamiento")]
        public async Task<IActionResult> Details(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var objeto = await dbContext.Objetos
                .Include(o => o.Suplidor)
                .FirstOrDefaultAsync(o => o.Id_parte == id);

            if (objeto == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Detalles de Almacenamiento", $"Se visualizaron los detalles del objeto con ID {id}.", fechaAccion, fechaAccion.TimeOfDay);
            }

            return View(objeto);
        }

        // GET: Almacenamiento/Edit/5
        [AutorizacionAccesoAtributo("EditarAlmacenamiento")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var objeto = await dbContext.Objetos.FindAsync(id);

            if (objeto == null)
            {
                return NotFound();
            }

            return View(objeto);
        }

        // POST: Almacenamiento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EditarAlmacenamiento")]
        public async Task<IActionResult> Edit(int id, [Bind("Id_parte,Nombre,Numero_parte,Descripcion,Cantidad_Disponible,Ubicacion,Fecha_Ingreso,Fecha_Vencimiento,SuplidorId,Archivo,MimeType,Activo")] Objetos objeto)
        {
            if (id != objeto.Id_parte)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using var dbContext = _contextFactory.CreateDbContext();
                try
                {
                    dbContext.Update(objeto);
                    await dbContext.SaveChangesAsync();

                    var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(perfilIdClaim, out int perfilId))
                    {
                        DateTime fechaAccion = DateTime.Now;
                        await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Actualizar Almacenamiento", $"Se actualizó el objeto con ID {id}.", fechaAccion, fechaAccion.TimeOfDay);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ObjetoExists(objeto.Id_parte))
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
            return View(objeto);
        }

        // GET: Almacenamiento/Disable/5 
        [AutorizacionAccesoAtributo("EliminarAlmacenamiento")]
        public async Task<IActionResult> Disable(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var objeto = await dbContext.Objetos.FirstOrDefaultAsync(o => o.Id_parte == id && o.Activo);

            if (objeto == null)
            {
                return NotFound();
            }

            return View(objeto);
        }

        // POST: Almacenamiento/Disable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarAlmacenamiento")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var objeto = await dbContext.Objetos.FirstOrDefaultAsync(o => o.Id_parte == id);
            if (objeto == null)
            {
                return NotFound();
            }

            objeto.Activo = false;

            dbContext.Update(objeto);
            await dbContext.SaveChangesAsync();

            var perfilIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(
                    perfilId,
                    User.Identity.Name,
                    "Deshabilitar Almacenamiento",
                    $"Se deshabilitó el objeto con ID {id}.",
                    fechaAccion,
                    horaAccion
                );
            }

            return RedirectToAction(nameof(Index));
        }

        //Metodo para exportar el reporte de almacenamiento a Excel
        [AutorizacionAccesoAtributo("ExcelAlmacenamiento")]
        public IActionResult ExportarExcel()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var datos = from o in dbContext.Objetos
                        join f in dbContext.Fabricantes on o.SuplidorId equals f.Id_suplidor
                        select new Almacenamiento
                        {
                            Id_parte = o.Id_parte,
                            Part = o.Nombre,
                            Numero_parte = o.Numero_parte,
                            Descripcion = o.Descripcion,
                            Cantidad_Disponible = (
                                (dbContext.Ingresos.Where(i => i.Id_parte == o.Id_parte).Sum(i => i.Cantidad))
                                - (dbContext.Egresos.Where(e => e.Id_parte == o.Id_parte).Sum(e => e.Cantidad))
                            ),
                            Ubicacion = o.Ubicacion,
                            Fecha_Ingreso = o.Fecha_Ingreso,
                            Fecha_Vencimiento = o.Fecha_Vencimiento,
                            Suplidor = f.Nombre_Suplidor,
                            Colateral = f.Colateral,
                            Archivo = o.Archivo, 
                            MimeType = o.MimeType,
                            Activo = o.Activo
                        };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReporteAlmacenamiento");

                worksheet.Cells[1, 1].Value = "ID Parte";
                worksheet.Cells[1, 2].Value = "Parte";
                worksheet.Cells[1, 3].Value = "Número Parte";
                worksheet.Cells[1, 4].Value = "Descripción";
                worksheet.Cells[1, 5].Value = "Cantidad Disponible";
                worksheet.Cells[1, 6].Value = "Ubicación";
                worksheet.Cells[1, 7].Value = "Fecha Ingreso";
                worksheet.Cells[1, 8].Value = "Fecha Vencimiento";
                worksheet.Cells[1, 9].Value = "Suplidor";
                worksheet.Cells[1, 10].Value = "Colateral";

                var row = 2;
                foreach (var item in datos.ToList())
                {
                    worksheet.Cells[row, 1].Value = item.Id_parte;
                    worksheet.Cells[row, 2].Value = item.Part;
                    worksheet.Cells[row, 3].Value = item.Numero_parte;
                    worksheet.Cells[row, 4].Value = item.Descripcion;
                    worksheet.Cells[row, 5].Value = item.Cantidad_Disponible;
                    worksheet.Cells[row, 6].Value = item.Ubicacion;
                    worksheet.Cells[row, 7].Value = item.Fecha_Ingreso?.ToShortDateString();
                    worksheet.Cells[row, 8].Value = item.Fecha_Vencimiento?.ToShortDateString();
                    worksheet.Cells[row, 9].Value = item.Suplidor;
                    worksheet.Cells[row, 10].Value = item.Colateral;
                    row++;
                }

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteAlmacenamiento.xlsx");
            }
        }

        //Metodo para exportar el reporte de almacenamiento a PDF
        [AutorizacionAccesoAtributo("PdfAlmacenamiento")]
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

            var datos = from o in dbContext.Objetos
                        join f in dbContext.Fabricantes on o.SuplidorId equals f.Id_suplidor
                        select new Almacenamiento
                        {
                            Id_parte = o.Id_parte,
                            Part = o.Nombre,
                            Numero_parte = o.Numero_parte,
                            Descripcion = o.Descripcion,
                            Cantidad_Disponible = (
                                (dbContext.Ingresos.Where(i => i.Id_parte == o.Id_parte).Sum(i => i.Cantidad))
                                - (dbContext.Egresos.Where(e => e.Id_parte == o.Id_parte).Sum(e => e.Cantidad))
                            ),
                            Ubicacion = o.Ubicacion,
                            Fecha_Ingreso = o.Fecha_Ingreso,
                            Fecha_Vencimiento = o.Fecha_Vencimiento,
                            Suplidor = f.Nombre_Suplidor,
                            Colateral = f.Colateral,
                            Archivo = o.Archivo,
                            MimeType = o.MimeType,
                            Activo = o.Activo
                        };

            var listaObjetos = datos.ToList();


            var html = @"
            <h1>Reporte de Almacenamiento</h1>
            <table border='1' cellpadding='5' cellspacing='0' width='100%'>
                <thead>
                    <tr>
                        <th>ID Parte</th>
                        <th>Parte</th>
                        <th>Número de Parte</th>
                        <th>Descripción</th>
                        <th>Cantidad Disponible</th>
                        <th>Ubicación</th>
                        <th>Fecha Ingreso</th>
                        <th>Fecha Vencimiento</th>
                        <th>Suplidor</th>
                        <th>Colateral</th>
                    </tr>
                </thead>
            <tbody>";

                    foreach (var item in listaObjetos)
                    {
                        html += $@"
            <tr>
                <td>{item.Id_parte}</td>
                <td>{item.Part}</td>
                <td>{item.Numero_parte}</td>
                <td>{item.Descripcion}</td>
                <td>{item.Cantidad_Disponible}</td>
                <td>{item.Ubicacion}</td>
                <td>{item.Fecha_Ingreso?.ToShortDateString()}</td>
                <td>{item.Fecha_Vencimiento?.ToShortDateString()}</td>
                <td>{item.Suplidor}</td>
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
            new ObjectSettings()
            {
                HtmlContent = html,
            }
        }
            };

            var pdf = converter.Convert(doc);

            return File(pdf, "application/pdf", "ReporteAlmacenamiento.pdf");
        }

        // GET: Almacenamiento/Descargar
        [AutorizacionAccesoAtributo("DescargarAlmacenamiento")]
        public async Task<IActionResult> Descargar(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var objeto = await dbContext.Objetos.FindAsync(id);

            if (objeto == null || objeto.Archivo == null || string.IsNullOrEmpty(objeto.MimeType) || !objeto.MimeType.StartsWith("image/"))
            {
                return NotFound("El archivo no existe o no es un formato de imagen válido.");
            }

            var extension = objeto.MimeType.Split('/').Last();

            var fileName = $"{objeto.Numero_parte}.{extension}";

            return File(objeto.Archivo, objeto.MimeType, fileName);
        }

        private bool ObjetoExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Objetos.Any(e => e.Id_parte == id);
        }
    }
}
