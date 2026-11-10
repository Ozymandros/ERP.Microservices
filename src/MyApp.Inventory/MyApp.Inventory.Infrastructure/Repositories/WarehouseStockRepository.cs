using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Inventory.Infrastructure.Repositories;

/// <summary>
/// Provides Warehouse Stock Repository functionality.
/// </summary>
public class WarehouseStockRepository : Repository<WarehouseStock, Guid>, IWarehouseStockRepository
{
    /// <summary>base.</summary>
    /// <param name="context">The context.</param>
    public WarehouseStockRepository(InventoryDbContext context) : base(context)
    {
    }

    /// <summary>Get By Product And Warehouse Async.</summary>
    /// <param name="productId">The product Id.</param>
    /// <param name="warehouseId">The warehouse Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<WarehouseStock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .FirstOrDefaultAsync(ws => ws.ProductId == productId && ws.WarehouseId == warehouseId, cancellationToken);
    }

    /// <summary>Get By Product Id Async.</summary>
    /// <param name="productId">The product Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<WarehouseStock>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Get By Warehouse Id Async.</summary>
    /// <param name="warehouseId">The warehouse Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<WarehouseStock>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Get Low Stock Async.</summary>
    /// <param name="reorderLevel">The reorder Level.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<List<WarehouseStock>> GetLowStockAsync(int? reorderLevel = null, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .AsQueryable();

        if (reorderLevel.HasValue)
        {
            query = query.Where(ws => ws.AvailableQuantity <= reorderLevel.Value);
        }
        else
        {
            query = query.Where(ws => ws.Product != null && ws.AvailableQuantity <= ws.Product.ReorderLevel);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
