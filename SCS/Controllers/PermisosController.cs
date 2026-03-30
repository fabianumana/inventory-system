using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCS.Autorizacion;
using SCS.Models;
using SCS.Services;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class PermisosController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;

        public PermisosController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
        }

        // GET: Permisos/Index
        [AutorizacionAccesoAtributo("IndexPermisos")]
        public async Task<IActionResult> Index(string nombre, bool? mostrarInactivos)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var permisos = dbContext.Permisos.AsQueryable();

            if (!string.IsNullOrEmpty(nombre))
            {
                permisos = permisos.Where(p => p.NombrePermiso.Contains(nombre));
            }

            if (!mostrarInactivos.HasValue || !mostrarInactivos.Value)
            {
                permisos = permisos.Where(p => p.Activo == true || p.Activo == null); 
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(
                    perfilId,
                    User.Identity.Name,
                    "Acceso a Index",
                    "Se accedió al listado de permisos.",
                    fechaAccion,
                    horaAccion
                );
            }
            var permisosList = await permisos.ToListAsync();

            ViewData["FiltroNombre"] = nombre;

            return View(permisosList);
        }

        // GET: Permisos/Details/5
        [AutorizacionAccesoAtributo("DetallesPermisos")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            using var dbContext = _contextFactory.CreateDbContext();
            var permiso = await dbContext.Permisos.FirstOrDefaultAsync(m => m.Id_permiso == id);

            if (permiso == null)
                return NotFound();

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Detalles", $"Se visualizó el permiso con ID {permiso.Id_permiso}.", DateTime.Now, DateTime.Now.TimeOfDay);
            }

            return View(permiso);
        }

        // GET: Permisos/Create
        [AutorizacionAccesoAtributo("CrearPermisos")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Permisos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("CrearPermisos")]
        public async Task<IActionResult> Create([Bind("Id_permiso,NombrePermiso,Descripcion")] Permisos permiso)
        {
            if (!ModelState.IsValid)
                return View(permiso);

            using var dbContext = _contextFactory.CreateDbContext();
            dbContext.Add(permiso);
            await dbContext.SaveChangesAsync();

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Crear", $"Se creó el permiso con ID {permiso.Id_permiso}.", DateTime.Now, DateTime.Now.TimeOfDay);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Permisos/Edit/5
        [AutorizacionAccesoAtributo("EditarPermisos")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var permiso = await dbContext.Permisos.FindAsync(id);
            if (permiso == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Editar", $"Se accedió a la edición del permiso con ID {permiso.Id_permiso}.", fechaAccion, horaAccion);
            }

            return View(permiso);
        }

        // POST: Permisos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EditarPermisos")]
        public async Task<IActionResult> Edit(int id, [Bind("Id_permiso,NombrePermiso,Descripcion")] Permisos permiso)
        {
            if (id != permiso.Id_permiso)
                return NotFound();

            if (!ModelState.IsValid)
                return View(permiso);

            using var dbContext = _contextFactory.CreateDbContext();
            try
            {
                dbContext.Update(permiso);
                await dbContext.SaveChangesAsync();

                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Editar", $"Se editó el permiso con ID {permiso.Id_permiso}.", DateTime.Now, DateTime.Now.TimeOfDay);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermisoExists(permiso.Id_permiso))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Permisos/Delete/5
        [AutorizacionAccesoAtributo("EliminarPermisos")]
        public async Task<IActionResult> Disable(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var permiso = await dbContext.Permisos.FirstOrDefaultAsync(m => m.Id_permiso == id);
            if (permiso == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Eliminar", $"Se accedió al formulario de eliminación del permiso con ID {permiso.Id_permiso}.", fechaAccion, horaAccion);
            }

            return View(permiso);
        }

        // POST: Permisos/DisableConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizacionAccesoAtributo("EliminarPermisos")]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var permiso = await dbContext.Permisos.FindAsync(id);

            if (permiso != null)
            {
                permiso.Activo = false;
                dbContext.Update(permiso);
                await dbContext.SaveChangesAsync();

                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Deshabilitar", $"Se deshabilitó el permiso con ID {id}.", DateTime.Now, DateTime.Now.TimeOfDay);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PermisoExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Permisos.Any(e => e.Id_permiso == id);
        }
    }
}
