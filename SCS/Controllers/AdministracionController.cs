using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCS.Autorizacion;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;


namespace SCS.Controllers
{
    public class AdministracionController : Controller
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly IDbContextFactory<Service> _dbContextFactory;
        private readonly BitacorasService _movimientoService;

        public AdministracionController(IFluentEmail fluentEmail, IDbContextFactory<Service> dbContextFactory, BitacorasService movimientoService)
        {
            _fluentEmail = fluentEmail;
            _dbContextFactory = dbContextFactory;
            _movimientoService = movimientoService;
        }

        // Método para obtener los roles aprobados del usuario
        private async Task<List<string>> ObtenerRolesAprobados(int userId)
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                return await _dbContext.UsuarioRoles
                    .Where(ur => ur.UsuarioId == userId && ur.IsApproved)
                    .Select(ur => ur.Rol.Rol)
                    .ToListAsync();
            }
        }

        // Método GET para solicitar roles
        [HttpGet]
        public async Task<IActionResult> SolicitarRol()
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var usuarios = await _dbContext.Perfiles.AsNoTracking().ToListAsync();
                var roles = await _dbContext.Roles
                    .AsNoTracking()
                    .Select(r => new Roles
                    {
                        Id_roles = r.Id_roles,
                        Rol = r.Rol,
                        Activo = r.Activo ?? true 
                    })
                    .ToListAsync();

                ViewBag.Perfiles = usuarios;
                ViewBag.Roles = roles;
                return View();
            }
        }

        // Método POST para solicitar roles
        [HttpPost]
        public async Task<IActionResult> SolicitarRol(int roleId)
        {
            if (roleId <= 0)
            {
                ViewData["Mensaje"] = "Rol no seleccionado.";
                return View();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                ViewData["Mensaje"] = "No se pudo obtener el ID del usuario.";
                return View();
            }

            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var rolExiste = await _dbContext.Roles.AnyAsync(r => r.Id_roles == roleId);
                if (!rolExiste)
                {
                    ViewData["Mensaje"] = "El rol no existe.";
                    return View();
                }

                var existingRequest = await _dbContext.UsuarioRoles
                    .AnyAsync(ur => ur.UsuarioId == currentUserId && ur.RolId == roleId && !ur.IsApproved);

                if (existingRequest)
                {
                    ViewData["Mensaje"] = "Ya has solicitado este rol y está pendiente de aprobación.";
                    return View();
                }

                var usuarioRole = new UsuarioRole
                {
                    UsuarioId = currentUserId,
                    RolId = roleId,
                    RequestedAt = DateTime.Now,
                    IsApproved = false
                };

                _dbContext.UsuarioRoles.Add(usuarioRole);
                await _dbContext.SaveChangesAsync();

                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _movimientoService.RegistrarMovimientoAsync(
                    currentUserId,
                    User.Identity.Name,
                    "Solicitud de Rol",
                    $"El usuario {currentUserId} solicitó el rol con ID {roleId}.",
                    fechaAccion,
                    horaAccion
                );

                var usuario = await _dbContext.Perfiles
                    .AsNoTracking()
                    .Where(p => p.Id_perfiles == currentUserId)
                    .Select(p => new { p.User, p.Correo })
                    .FirstOrDefaultAsync();

                var rolSolicitado = await _dbContext.Roles
                    .AsNoTracking()
                    .Where(r => r.Id_roles == roleId)
                    .Select(r => r.Rol)
                    .FirstOrDefaultAsync();

                if (usuario == null || string.IsNullOrEmpty(rolSolicitado))
                {
                    ViewData["Mensaje"] = "No se pudo obtener información del usuario o del rol.";
                    return View();
                }

                var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Rol == "Administrador");

                if (adminRole == null)
                {
                    ViewData["Mensaje"] = "No se encontró el rol de administrador.";
                    return View();
                }

                var adminEmail = await _dbContext.Perfiles
                    .AsNoTracking()
                    .Where(p => p.Id_rol == adminRole.Id_roles)
                    .Select(p => p.Correo)
                    .FirstOrDefaultAsync();

                if (adminEmail != null)
                {
                    try
                    {
                        await _fluentEmail
                            .To(adminEmail)
                            .Subject("Solicitud de rol de usuario")
                            .Body($"El usuario {usuario.User} ha solicitado el rol '{rolSolicitado}'. Por favor, revisa la solicitud en el sistema.")
                            .SendAsync();

                        ViewData["Mensaje"] = "Solicitud de rol enviada exitosamente. Espera la aprobación del administrador.";
                    }
                    catch (Exception ex)
                    {
                        ViewData["Mensaje"] = $"No se pudo enviar la notificación al administrador. Error: {ex.Message}";
                    }
                }
                else
                {
                    ViewData["Mensaje"] = "No se pudo encontrar el correo electrónico del administrador.";
                }

                return View();
            }
        }

        //Metodo para revisar solicitudes
        [HttpGet]
        [AutorizacionAccesoAtributo("RevisarSolicitud")]
        public async Task<IActionResult> RevisarSolicitudes()
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var solicitudes = await _dbContext.UsuarioRoles
                    .AsNoTracking()
                    .Include(ur => ur.Usuario)
                    .Include(ur => ur.Rol)
                    .Where(ur => !ur.IsApproved)
                    .ToListAsync();

                return View(solicitudes);
            }
        }

        //Metodo que me asigna todos los permisos que existen al rol Administrador
        [AutorizacionAccesoAtributo("AsignarPermiso")]
        public async Task AsignarPermisosAdmin(int rolId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var rol = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id_roles == rolId);

            if (rol != null && rol.Rol == "Administrador")
            {
                var permisosExcluidos = new List<string>
                {
                    "MenuRepotesUsuario",
                    "MenuReportesVisor",
                    "MenuInicioUsuario",
                    "MenuInicioVisor"
                };

                var permisosAsignables = await dbContext.Permisos
                        .Where(p => !permisosExcluidos.Contains(p.NombrePermiso))
                        .ToListAsync();

                foreach (var permiso in permisosAsignables)
                {
                    var rolePermiso = new RolesPermisos
                    {
                        RolId = rolId,
                        PermisoId = permiso.Id_permiso
                    };

                    dbContext.RolePermisos.Add(rolePermiso);
                }

                await dbContext.SaveChangesAsync();
            }
        }

        // POST: Procesar solicitudes aprobadas y rechazadas
        [HttpPost]
        [AutorizacionAccesoAtributo("ProcesarSolicitud")]
        public async Task<IActionResult> ProcesarSolicitudes(List<string> aprobadas, List<string> rechazadas)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                if ((aprobadas == null || aprobadas.Count == 0) && (rechazadas == null || rechazadas.Count == 0))
                {
                    ViewData["Mensaje"] = "No se seleccionó ninguna solicitud para procesar.";
                    return RedirectToAction("RevisarSolicitudes");
                }

                var conflictos = aprobadas?.Intersect(rechazadas).ToList();
                if (conflictos != null && conflictos.Any())
                {
                    ViewData["Mensaje"] = "No puedes aprobar y rechazar la misma solicitud.";
                    return RedirectToAction("RevisarSolicitudes");
                }

                List<string> erroresCorreo = new List<string>();

                if (aprobadas != null && aprobadas.Count > 0)
                {
                    foreach (var solicitudStr in aprobadas)
                    {
                        var partes = solicitudStr.Split('-');
                        if (partes.Length == 2 && int.TryParse(partes[0], out int usuarioId) && int.TryParse(partes[1], out int rolId))
                        {
                            var solicitud = await dbContext.UsuarioRoles
                                .FirstOrDefaultAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == rolId && !ur.IsApproved);

                            if (solicitud != null)
                            {
                                solicitud.IsApproved = true;

                                var usuario = await dbContext.Perfiles.FirstOrDefaultAsync(p => p.Id_perfiles == solicitud.UsuarioId);
                                if (usuario != null)
                                {
                                    usuario.Id_rol = solicitud.RolId;

                                    var rolAsignado = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id_roles == solicitud.RolId);
                                    if (rolAsignado?.Rol == "Administrador")
                                    {
                                        await AsignarPermisosAdmin(rolAsignado.Id_roles);
                                    }

                                    await _movimientoService.RegistrarMovimientoAsync( usuarioId,User.Identity.Name, "Aprobación de Rol", $"El rol {rolAsignado.Rol} fue aprobado para el usuario {usuario.User}.", DateTime.Now, DateTime.Now.TimeOfDay);

                                    if (!string.IsNullOrEmpty(usuario.Correo) && rolAsignado != null)
                                    {
                                        try
                                        {
                                            await _fluentEmail
                                                .To(usuario.Correo)
                                                .Subject("Solicitud de rol aprobada")
                                                .Body($"Estimado {usuario.User}, tu solicitud para el rol '{rolAsignado.Rol}' ha sido aprobada.")
                                                .SendAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            erroresCorreo.Add($"Error al enviar correo de aprobación a {usuario.Correo}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (rechazadas != null && rechazadas.Count > 0)
                {
                    foreach (var solicitudStr in rechazadas)
                    {
                        var partes = solicitudStr.Split('-');
                        if (partes.Length == 2 && int.TryParse(partes[0], out int usuarioId) && int.TryParse(partes[1], out int rolId))
                        {
                            var solicitud = await dbContext.UsuarioRoles
                                .FirstOrDefaultAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == rolId && !ur.IsApproved);

                            if (solicitud != null)
                            {
                                dbContext.UsuarioRoles.Remove(solicitud);

                                var usuario = await dbContext.Perfiles.FirstOrDefaultAsync(p => p.Id_perfiles == solicitud.UsuarioId);
                                if (usuario != null && !string.IsNullOrEmpty(usuario.Correo))
                                {
                                    try
                                    {
                                        await _fluentEmail
                                            .To(usuario.Correo)
                                            .Subject("Solicitud de rol rechazada")
                                            .Body($"Estimado {usuario.User}, lamentamos informarte que tu solicitud para el rol ha sido rechazada.")
                                            .SendAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        erroresCorreo.Add($"Error al enviar correo de rechazo a {usuario.Correo}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync();

                if (erroresCorreo.Any())
                {
                    ViewData["Mensaje"] = $"Solicitudes procesadas, pero algunos correos no se pudieron enviar: {string.Join(", ", erroresCorreo)}";
                }
                else
                {
                    ViewData["Mensaje"] = "Solicitudes procesadas correctamente.";
                }

                return RedirectToAction("RevisarSolicitudes");
            }
        }

        //Metodo que muestra los detalles del usuario
        [HttpGet]
        public async Task<IActionResult> DetallesUsuario()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                ViewData["Mensaje"] = "No se pudo obtener el ID del usuario.";
                return RedirectToAction("Index", "Home");
            }

            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var perfil = await _dbContext.Perfiles
                    .AsNoTracking()
                    .Include(p => p.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                    .FirstOrDefaultAsync(p => p.Id_perfiles == userId);

                if (perfil == null)
                {
                    ViewData["Mensaje"] = "Perfil no encontrado.";
                    return RedirectToAction("Index", "Home");
                }

                return View(perfil);
            }
        }
    }
}
