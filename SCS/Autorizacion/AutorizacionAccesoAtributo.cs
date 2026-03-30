using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SCS.Services;

namespace SCS.Autorizacion
{
    public class AutorizacionAccesoAtributo : TypeFilterAttribute
    {
        public AutorizacionAccesoAtributo(params string[] permisos) : base(typeof(AutorizacionAccesoFiltro))
        {
            Arguments = new object[] { permisos };
        }

        public class AutorizacionAccesoFiltro : IAuthorizationFilter
        {
            private readonly string[] _permisos;
            private readonly IDbContextFactory<Service> _dbContextFactory;

            public AutorizacionAccesoFiltro(string[] permisos, IDbContextFactory<Service> dbContextFactory)
            {
                _permisos = permisos;
                _dbContextFactory = dbContextFactory;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null || !int.TryParse(userId, out int parsedUserId))
                {
                    context.Result = new RedirectToActionResult("Login", "Acceso", null);
                    return;
                }

                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    var esAdmin = await dbContext.UsuarioRoles
                        .AnyAsync(ur => ur.UsuarioId == parsedUserId && ur.IsApproved && ur.Rol.Rol == "Administrador");

                    if (esAdmin)
                    {
                        return;
                    }

                    var permisosUsuario = await dbContext.RolePermisos
                        .Where(rp => dbContext.UsuarioRoles
                            .Where(ur => ur.UsuarioId == parsedUserId && ur.IsApproved)
                            .Select(ur => ur.RolId)
                            .Contains(rp.RolId))
                        .Select(rp => rp.Permiso.NombrePermiso)
                        .ToListAsync();

                    var tienePermiso = _permisos.All(permiso => permisosUsuario.Contains(permiso));

                    if (!tienePermiso)
                    {
                        context.Result = new ForbidResult();
                    }
                }
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                OnAuthorizationAsync(context).Wait();
            }
        }
    }
}
