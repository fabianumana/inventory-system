using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SCS.Autorizacion;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class RolesController : Controller
    {
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _bitacorasService;

        public RolesController(IDbContextFactory<Service> contextFactory, BitacorasService bitacorasService)
        {
            _contextFactory = contextFactory;
            _bitacorasService = bitacorasService;
        }

        // GET: Roles/Index
        [AutorizacionAccesoAtributo("IndexRoles")]
        public async Task<IActionResult> Index(int? id, string nombre)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var rolesQuery = dbContext.Roles.AsQueryable();

            if (id.HasValue && id > 0)
            {
                rolesQuery = rolesQuery.Where(r => r.Id_roles == id.Value);
            }

            if (!string.IsNullOrEmpty(nombre))
            {
                rolesQuery = rolesQuery.Where(r => r.Rol.Contains(nombre));
            }

            rolesQuery = rolesQuery.Where(r => r.Activo == true || r.Activo == null);

            var roles = await rolesQuery.ToListAsync();

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Index", "Accedió al listado de roles", fechaAccion, horaAccion);
            }

            return View(roles);
        }

        // GET: Roles/Details/5
        [AutorizacionAccesoAtributo("DetallesRoles")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var roles = await dbContext.Roles.FirstOrDefaultAsync(m => m.Id_roles == id);
            if (roles == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Visualizar", $"Se visualizó el rol con ID {roles.Id_roles}", fechaAccion, horaAccion);
            }

            return View(roles);
        }

        // GET: Roles/Create
        [AutorizacionAccesoAtributo("CrearRoles")]
        public IActionResult Create()
        {
            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso al Formulario de Creación", "El usuario accedió al formulario de creación de un nuevo rol", fechaAccion, horaAccion);
            }

            return View();
        }

        // POST: Roles/Create
        [AutorizacionAccesoAtributo("CrearRoles")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Rol")] Roles roles)
        {
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
                return View(roles);
            }

            using var dbContext = _contextFactory.CreateDbContext();
            dbContext.Add(roles);
            await dbContext.SaveChangesAsync();

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Crear", $"Se creó un nuevo rol: {roles.Rol}", fechaAccion, horaAccion);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Roles/Edit/5
        [AutorizacionAccesoAtributo("EditarRoles")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var roles = await dbContext.Roles.FindAsync(id);
            if (roles == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso a Edición", $"Se accedió al formulario de edición para el rol con ID {roles.Id_roles}", fechaAccion, horaAccion);
            }

            return View(roles);
        }

        // POST: Roles/Edit/5
        [AutorizacionAccesoAtributo("EditarRoles")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_roles,Rol")] Roles roles)
        {
            if (id != roles.Id_roles)
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
                return View(roles);
            }

            try
            {
                using var dbContext = _contextFactory.CreateDbContext();
                dbContext.Update(roles);
                await dbContext.SaveChangesAsync();

                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Actualizar", $"Se actualizó el rol con ID {roles.Id_roles}", fechaAccion, horaAccion);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolesExists(roles.Id_roles))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: Roles/Delete/5
        [AutorizacionAccesoAtributo("EliminarRoles")]
        public async Task<IActionResult> Disable(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            using var dbContext = _contextFactory.CreateDbContext();
            var roles = await dbContext.Roles.FirstOrDefaultAsync(m => m.Id_roles == id);
            if (roles == null)
            {
                return NotFound();
            }

            var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(perfilIdClaim, out int perfilId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Acceso al Formulario de Eliminación", $"Accedió al formulario de eliminación del rol con ID {roles.Id_roles}", fechaAccion, horaAccion);
            }

            return View(roles);
        }

        // POST: Roles/Disable/5 
        [HttpPost]
        [AutorizacionAccesoAtributo("EliminarRoles")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableConfirmed(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var roles = await dbContext.Roles.FindAsync(id);

            if (roles == null)
            {
                ModelState.AddModelError("", $"El rol con ID {id} no fue encontrado.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                roles.Activo = false;
                dbContext.Entry(roles).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                var perfilIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(perfilIdClaim, out int perfilId))
                {
                    DateTime fechaAccion = DateTime.Now;
                    TimeSpan horaAccion = fechaAccion.TimeOfDay;
                    await _bitacorasService.RegistrarMovimientoAsync(perfilId, User.Identity.Name, "Deshabilitar", $"Se deshabilitó el rol con ID {roles.Id_roles}", fechaAccion, horaAccion);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al deshabilitar el rol: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RolesExists(int id)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return dbContext.Roles.Any(e => e.Id_roles == id);
        }

        //Metodo GET ManejarPermisos
        [HttpGet]
        [AutorizacionAccesoAtributo("ManejarPermisos")]
        public async Task<IActionResult> ManejarPermisos(int rolId)
        {
            if (rolId <= 0)
            {
                ModelState.AddModelError("", "El ID del rol no es válido.");
                return View("Error");
            }

            using (var dbContext = _contextFactory.CreateDbContext())
            {
                var rol = await dbContext.Roles
                    .Include(r => r.RolePermisos)
                    .ThenInclude(rp => rp.Permiso)
                    .FirstOrDefaultAsync(r => r.Id_roles == rolId);

                if (rol == null)
                {
                    ModelState.AddModelError("", "El rol no existe.");
                    return View("Error");
                }

                var permisos = await dbContext.Permisos.ToListAsync();

                if (permisos == null || !permisos.Any())
                {
                    ModelState.AddModelError("", "No hay permisos disponibles para asignar.");
                    return View("Error");
                }

                var viewModel = new RolesPermisoVM
                {
                    RolId = rol.Id_roles,
                    RolNombre = rol.Rol ?? "Sin nombre",
                    Permisos = permisos.Select(p => new PermisosVM
                    {
                        PermisoId = p.Id_permiso,
                        PermisoNombre = p.NombrePermiso ?? "Sin nombre",
                        Asignado = rol.RolePermisos.Any(rp => rp.PermisoId == p.Id_permiso)
                    }).ToList()
                };

                return View(viewModel);
            }
        }

        //ManejarPermisos - Post
        [HttpPost]
        [AutorizacionAccesoAtributo("ManejarPermisos")]
        public async Task<IActionResult> ManejarPermisos(RolesPermisoVM model)
        {
            using (var dbContext = _contextFactory.CreateDbContext())
            {
                if (model.PermisosSeleccionados == null || !model.PermisosSeleccionados.Any())
                {
                    ModelState.AddModelError("", "No se seleccionaron permisos.");
                    return View(model);
                }

                try
                {
                    var rol = await dbContext.Roles
                        .Include(r => r.RolePermisos)
                        .FirstOrDefaultAsync(r => r.Id_roles == model.RolId);

                    if (rol == null)
                    {
                        ModelState.AddModelError("", "Rol no encontrado.");
                        return RedirectToAction("Index");
                    }

                    dbContext.RolePermisos.RemoveRange(rol.RolePermisos);

                    foreach (var permisoId in model.PermisosSeleccionados)
                    {
                        dbContext.RolePermisos.Add(new RolesPermisos
                        {
                            RolId = rol.Id_roles,
                            PermisoId = permisoId
                        });
                    }

                    await dbContext.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al asignar permisos: {ex.Message}");
                    return View(model);
                }
            }
        }
    }
}
