using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Warehouse Repository functionality.
/// </summary>
public class WarehouseRepository : Repository<Warehouse, Guid>, IWarehouseRepository
{
    /// <summary>base.</summary>
    public WarehouseRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Name Async.</summary>
    public async Task<Warehouse?> GetByNameAsync(string name)
    {
        return await DbContext.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Name == name);
    }
}
