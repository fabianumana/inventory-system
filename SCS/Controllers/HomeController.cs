using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCS.Models;
using SCS.Services;
using SCS.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace SCS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDbContextFactory<Service> _contextFactory;
        private readonly BitacorasService _movimientoService;

        public HomeController(ILogger<HomeController> logger, IDbContextFactory<Service> contextFactory, BitacorasService movimientoService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _movimientoService = movimientoService;
        }

        private async Task<List<string>> ObtenerRolesAprobados(int userId)
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.UsuarioRoles
                .Where(ur => ur.UsuarioId == userId && ur.IsApproved)
                .Select(ur => ur.Rol.Rol)
                .ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
            {
                return RedirectToAction("Registrar", "Acceso");
            }

            var userRoles = await ObtenerRolesAprobados(parsedUserId);
            ViewBag.UserRoles = userRoles;

            DateTime fechaAccion = DateTime.Now;
            TimeSpan horaAccion = fechaAccion.TimeOfDay;

            await _movimientoService.RegistrarMovimientoAsync(
                parsedUserId,
                User.Identity.Name,
                "Acceso a la página de inicio",
                "El usuario accedió a la página de inicio.",
                fechaAccion,
                horaAccion
            );

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Logout()
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuarioNombre = User.FindFirstValue(ClaimTypes.Name);

            var perfil = await dbContext.Perfiles
                .Where(p => p.Id_perfiles.ToString() == usuarioId)
                .FirstOrDefaultAsync();

            if (perfil == null)
            {
                return RedirectToAction("Error", "Home");
            }

            await _movimientoService.RegistrarEntradaSalidaAsync(perfil.Id_perfiles, perfil.User, "Salida", null, DateTime.Now, null, DateTime.Now.TimeOfDay);

            DateTime fechaAccion = DateTime.Now;
            TimeSpan horaAccion = fechaAccion.TimeOfDay;
            await _movimientoService.RegistrarMovimientoAsync(
                int.Parse(usuarioId),
                User.Identity.Name,
                "Cerrar sesión",
                $"El usuario {usuarioNombre} cerró sesión.",
                fechaAccion,
                horaAccion
            );

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }

        public async Task<IActionResult> Inicio()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Acceso");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
            {
                return RedirectToAction("Login", "Acceso");
            }

            var userRoles = await ObtenerRolesAprobados(parsedUserId);
            ViewBag.UserRoles = userRoles;

            using (var dbContext = _contextFactory.CreateDbContext())
            {
                var permisosUsuario = await dbContext.RolePermisos
                    .Where(rp => dbContext.UsuarioRoles
                        .Where(ur => ur.UsuarioId == parsedUserId && ur.IsApproved)
                        .Select(ur => ur.RolId)
                        .Contains(rp.RolId))
                    .Select(rp => rp.Permiso.NombrePermiso)
                    .ToListAsync();

                ViewBag.UserPermisos = permisosUsuario;

                var existenFabricantes = await dbContext.Fabricantes.AnyAsync(f => f.Activo);
                ViewBag.ExistenFabricantes = existenFabricantes;

                var existenObjetos = await dbContext.Objetos.AnyAsync(o => o.Activo);
                ViewBag.ExistenObjetos = existenObjetos;
            }

            DateTime fechaAccion = DateTime.Now;
            TimeSpan horaAccion = fechaAccion.TimeOfDay;
            await _movimientoService.RegistrarMovimientoAsync(
                parsedUserId,
                User.Identity.Name,
                "Acceso a la página de inicio",
                "El usuario accedió a la página de inicio.",
                fechaAccion,
                horaAccion
            );

            return View();
        }

        public async Task<IActionResult> AcercaDe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int parsedUserId))
            {
                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _movimientoService.RegistrarMovimientoAsync(
                    parsedUserId,
                    User.Identity.Name,
                    "Acceso a la página 'Acerca de'",
                    "El usuario accedió a la página 'Acerca de'.",
                    fechaAccion,
                    horaAccion
                );
            }

            return View();
        }

        public async Task<IActionResult> MenuReportes()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return RedirectToAction("Error", "Home");
                }

                var userRoles = await ObtenerRolesAprobados(parsedUserId);
                ViewBag.UserRoles = userRoles;

                using (var dbContext = _contextFactory.CreateDbContext())
                {
                    var permisosUsuario = await dbContext.RolePermisos
                        .Where(rp => dbContext.UsuarioRoles
                            .Where(ur => ur.UsuarioId == parsedUserId && ur.IsApproved)
                            .Select(ur => ur.RolId)
                            .Contains(rp.RolId))
                        .Select(rp => rp.Permiso.NombrePermiso)
                        .ToListAsync();

                    ViewBag.UserPermisos = permisosUsuario;
                }

                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;
                await _movimientoService.RegistrarMovimientoAsync(
                    parsedUserId,
                    User.Identity.Name,
                    "Acceso al menú de reportes",
                    "El usuario accedió al menú de reportes.",
                    fechaAccion,
                    horaAccion
                );
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home");
            }

            return View();
        }

        public IActionResult Ayuda()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Pdf/ManualDeUsuario.pdf");
            return PhysicalFile(filePath, "application/pdf", "ManualDeUsuario.pdf");
        }

        public async Task<IActionResult> Seguridad()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
            {
                return RedirectToAction("Error", "Home");
            }

            var userRoles = await ObtenerRolesAprobados(parsedUserId);
            ViewBag.UserRoles = userRoles;

            DateTime fechaAccion = DateTime.Now;
            TimeSpan horaAccion = fechaAccion.TimeOfDay;
            await _movimientoService.RegistrarMovimientoAsync(
                parsedUserId,
                User.Identity.Name,
                "Acceso a la página de seguridad",
                "El usuario accedió a la página de seguridad.",
                fechaAccion,
                horaAccion
            );

            return View();
        }
    }
}
