using FluentEmail.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCS.Services;
using SCS.ViewModels;
using System.Security.Claims;
using SCS.Models;

namespace SCS.Controllers
{
    public class AccesoController : Controller
    {
        private readonly IDbContextFactory<Service> _dbContextFactory;
        private readonly IFluentEmail _emailSender;
        private readonly BitacorasService _bitacoraService;

        public AccesoController(IFluentEmail emailSender, IDbContextFactory<Service> dbContextFactory, BitacorasService bitacoraService)
        {
            _dbContextFactory = dbContextFactory;
            _emailSender = emailSender;
            _bitacoraService = bitacoraService;
        }

        // Método GET de Registrar
        [HttpGet]
        public IActionResult Registrar()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Método para validar contraseña unica
        private async Task<bool> ContrasenaUnica(string password)
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var passwordHasher = new PasswordHasher<Usuarios>();
                var hashedPassword = passwordHasher.HashPassword(null, password);

                var exists = await _dbContext.Perfiles
                    .AnyAsync(p => p.Contrasena == hashedPassword);

                return !exists;
            }
        }

        // Método para validar correo unico
        private async Task<bool> CorreoUnico(string correo)
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var exists = await _dbContext.Perfiles
                    .AnyAsync(p => p.Correo == correo);

                return !exists;
            }
        }

        // Método para validar ID del trabajador unico
        private async Task<bool> WWIDUnico(string wwid)
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var exists = await _dbContext.Perfiles
                    .AnyAsync(p => p.WWID == wwid);

                return !exists;
            }
        }

        // Método POST de registrar 
        [HttpPost]
        public async Task<IActionResult> Registrar(RegistrarVM modelo)
        {
            if (!ModelState.IsValid)
            {
                if (!await ContrasenaUnica(modelo.Contrasena))
                {
                    ModelState.AddModelError(nameof(modelo.Contrasena), "La contraseña ya está en uso. Por favor, elige una contraseña diferente.");
                }

                if (!await CorreoUnico(modelo.Correo))
                {
                    ModelState.AddModelError(nameof(modelo.Correo), "El correo ya está registrado. Por favor, utiliza un correo diferente.");
                }

                if (!await WWIDUnico(modelo.WWID))
                {
                    ModelState.AddModelError(nameof(modelo.WWID), "El WWID ya está registrado. Por favor, utiliza un WWID diferente.");
                }

                if (!ModelState.IsValid)
                {
                    return View(modelo);
                }
            }

            var passwordHasher = new PasswordHasher<Usuarios>();
            string hashedPassword = passwordHasher.HashPassword(null, modelo.Contrasena);

            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var usuario = new Usuarios
                {
                    User = modelo.Usuario,
                    Correo = modelo.Correo,
                    WWID = modelo.WWID,
                    Contrasena = hashedPassword,
                    Confirmacion = hashedPassword,
                    Apellidos = modelo.Apellidos,
                    Telefono = modelo.Telefono,
                    Departamento = modelo.Departamento,
                    Superior = modelo.Superior,
                    Id_rol = null
                };

                await _dbContext.Perfiles.AddAsync(usuario);
                await _dbContext.SaveChangesAsync();

                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacoraService.RegistrarMovimientoAsync(usuario.Id_perfiles, usuario.User, "Registro", $"Se registró un nuevo usuario con el correo {usuario.Correo}.", fechaAccion, horaAccion);

                await _bitacoraService.RegistrarEntradaSalidaAsync(
                    usuario.Id_perfiles, usuario.User, "Entrada", DateTime.Now, null, DateTime.Now.TimeOfDay, null);

                var correosAdmin = new List<string> { "fabianmj1236@gmail.com", "nathalia@gmail.com", "gabriela@gmail.com" };
                if (correosAdmin.Contains(usuario.Correo))
                {
                    var rolAdmin = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Rol == "Administrador");
                    if (rolAdmin != null)
                    {
                        usuario.Id_rol = rolAdmin.Id_roles;

                        var usuarioRol = new UsuarioRole
                        {
                            UsuarioId = usuario.Id_perfiles,
                            RolId = rolAdmin.Id_roles,
                            RequestedAt = DateTime.Now,
                            IsApproved = true
                        };
                        _dbContext.UsuarioRoles.Add(usuarioRol);

                        var permisosExcluidos = new List<string>
                        {
                            "MenuReportesUsuario",
                            "MenuReportesVisor",
                            "MenuInicioUsuario",
                            "MenuInicioVisor"
                        };

                        var permisosAdmin = await _dbContext.Permisos
                            .Where(p => !permisosExcluidos.Contains(p.NombrePermiso))
                            .ToListAsync();

                        foreach (var permiso in permisosAdmin)
                        {
                            if (!await _dbContext.RolePermisos.AnyAsync(rp => rp.RolId == rolAdmin.Id_roles && rp.PermisoId == permiso.Id_permiso))
                            {
                                _dbContext.RolePermisos.Add(new RolesPermisos
                                {
                                    RolId = rolAdmin.Id_roles,
                                    PermisoId = permiso.Id_permiso
                                });
                            }
                        }

                        await _dbContext.SaveChangesAsync();

                        await _bitacoraService.RegistrarMovimientoAsync(
                            usuario.Id_perfiles,
                            usuario.User,
                            "Asignación de Rol",
                            $"Se asignó automáticamente el rol 'Administrador' al usuario {usuario.User}.",
                            fechaAccion,
                            horaAccion
                        );
                    }
                }

                if (usuario.Id_perfiles != 0)
                {
                    TempData["Mensaje"] = "Registro exitoso. Puedes iniciar sesión ahora.";
                    return RedirectToAction("Login", "Acceso");
                }

                ViewData["Mensaje"] = "No se pudo crear el usuario. Por favor, inténtalo de nuevo.";
                return View();
            }
        }

        // Método GET del Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Método POST del Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                var perfil = await _dbContext.Perfiles
                    .Where(p => p.Correo == modelo.Correo)
                    .FirstOrDefaultAsync();

                if (perfil == null)
                {
                    ViewData["Mensaje"] = "No se encontraron coincidencias con el correo.";
                    return View(modelo);
                }

                if (!perfil.Activo)
                {
                    ViewData["Mensaje"] = "Este usuario está deshabilitado. Contacta al administrador.";
                    return View(modelo);
                }

                var passwordHasher = new PasswordHasher<Usuarios>();
                var verificationResult = passwordHasher.VerifyHashedPassword(perfil, perfil.Contrasena, modelo.Contrasena);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    ViewData["Mensaje"] = "Contraseña incorrecta.";
                    return View(modelo);
                }

                var rolesAprobados = await ObtenerRolesAprobados(perfil.Id_perfiles);

                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, perfil.User),
                    new Claim(ClaimTypes.NameIdentifier, perfil.Id_perfiles.ToString())
                };

                foreach (var role in rolesAprobados)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                DateTime fechaAccion = DateTime.Now;
                TimeSpan horaAccion = fechaAccion.TimeOfDay;

                await _bitacoraService.RegistrarEntradaSalidaAsync(perfil.Id_perfiles, perfil.User, "Entrada", DateTime.Now.Date, null, DateTime.Now.TimeOfDay, null);
                await _bitacoraService.RegistrarMovimientoAsync(perfil.Id_perfiles, perfil.User, "Inicio de Sesión", "El usuario inició sesión exitosamente.", fechaAccion, horaAccion);

                if (rolesAprobados.Any())
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("SolicitarRol", "Administracion");
                }
            }
        }

        // Método para obtener los roles aprobados del usuario
        private async Task<List<string>> ObtenerRolesAprobados(int perfilId)
        {
            using (var _dbContext = _dbContextFactory.CreateDbContext())
            {
                return await _dbContext.UsuarioRoles
                    .Where(ur => ur.UsuarioId == perfilId && ur.IsApproved)
                    .Select(ur => ur.Rol.Rol)
                    .ToListAsync();
            }
        }
    }
}
