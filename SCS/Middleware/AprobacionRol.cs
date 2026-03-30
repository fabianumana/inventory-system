using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SCS.Services;
using System.Security.Claims;

namespace SCS.Middleware
{
    public class AprobacionRol
    {
        private readonly RequestDelegate _next;
        private readonly IDbContextFactory<Service> _contextFactory;

        public AprobacionRol(RequestDelegate next, IDbContextFactory<Service> contextFactory)
        {
            _next = next;
            _contextFactory = contextFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    using (var dbContext = _contextFactory.CreateDbContext())
                    {
                        var rolesAprobados = await dbContext.UsuarioRoles
                            .AsNoTracking()
                            .Where(ur => ur.UsuarioId == userId && ur.IsApproved)
                            .Select(ur => ur.Rol.Rol)
                            .ToListAsync();

                        var permisosAprobados = await dbContext.UsuarioRoles
                            .AsNoTracking()
                            .Where(ur => ur.UsuarioId == userId && ur.IsApproved)
                            .SelectMany(ur => ur.Rol.RolePermisos)
                            .Select(rp => rp.Permiso.NombrePermiso)
                            .ToListAsync();

                        var claims = rolesAprobados.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
                        claims.AddRange(permisosAprobados.Select(permiso => new Claim("Permiso", permiso)));

                        if (rolesAprobados.Contains("Administrador"))
                        {
                            var todosLosPermisos = await dbContext.Permisos
                                .Select(p => p.NombrePermiso)
                                .ToListAsync();

                            claims.AddRange(todosLosPermisos.Select(permiso => new Claim("Permiso", permiso)));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(claimsIdentity);

                        context.User = principal;
                    }
                }
            }

            await _next(context);
        }
    }
}
