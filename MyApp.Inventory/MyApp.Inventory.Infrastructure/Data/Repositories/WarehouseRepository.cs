using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Infrastructure.Data.Repositories;

public class WarehouseRepository : Repository<Warehouse, Guid>, IWarehouseRepository
{
    public WarehouseRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Warehouse?> GetByNameAsync(string name)
    {
        return await _dbContext.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Name == name);
    }
}
