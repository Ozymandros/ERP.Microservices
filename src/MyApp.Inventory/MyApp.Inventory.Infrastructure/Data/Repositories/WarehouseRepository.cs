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
    /// <param name="dbContext">The db Context.</param>
    public WarehouseRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Name Async.</summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Warehouse>()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }
}
