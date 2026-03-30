using Microsoft.EntityFrameworkCore;
using SCS.Services;

namespace SCS.Middleware
{
    public class RedirigirRegistrar
    {
        private readonly RequestDelegate _next;
        private readonly IDbContextFactory<Service> _contextFactory;

        public RedirigirRegistrar(RequestDelegate next, IDbContextFactory<Service> contextFactory)
        {
            _next = next;
            _contextFactory = contextFactory;
        }

        public async Task InvokeAsync(HttpContext contexto)
        {
            var allowedPaths = new[]
            {
                "/Acceso/Registrar",
                "/Acceso/Login",
                "/Home/Index",
                "/favicon.ico",
                "/css/",
                "/js/",
                "/images/",
            };

            bool isAllowedPath = allowedPaths.Any(path => contexto.Request.Path.StartsWithSegments(path));

            if (contexto.User.Identity.IsAuthenticated && !isAllowedPath)
            {
                using (var dbContext = _contextFactory.CreateDbContext())
                {
                    contexto.Response.Redirect("/Acceso/Registrar");
                    return;
                }
            }

            await _next(contexto);
        }
    }
}
