using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace SCS.Services
{
    public class FabricantesService
    {
        private readonly IDbContextFactory<Service> _contextFactory;

        public FabricantesService(IDbContextFactory<Service> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SelectListItem>> GetFabricantesActivosAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.Fabricantes
                .Where(f => f.Activo)
                .Select(f => new SelectListItem
                {
                    Value = f.Id_suplidor.ToString(), 
                    Text = f.Nombre_Suplidor 
                })
                .ToListAsync();
        }
    }
}
