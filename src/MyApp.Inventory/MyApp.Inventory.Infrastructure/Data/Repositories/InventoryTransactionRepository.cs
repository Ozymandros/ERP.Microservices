using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Inventory Transaction Repository functionality.
/// </summary>
public class InventoryTransactionRepository : Repository<InventoryTransaction, Guid>, IInventoryTransactionRepository
{
    /// <summary>base.</summary>
    public InventoryTransactionRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Product Id Async.</summary>
    public async Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId)
    {
        return await _dbContext.Set<InventoryTransaction>()
            .Where(x => x.ProductId == productId)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }

    /// <summary>Get By Warehouse Id Async.</summary>
    public async Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        return await _dbContext.Set<InventoryTransaction>()
            .Where(x => x.WarehouseId == warehouseId)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }

    /// <summary>Get By Transaction Type Async.</summary>
    public async Task<IEnumerable<InventoryTransaction>> GetByTransactionTypeAsync(TransactionType transactionType)
    {
        return await _dbContext.Set<InventoryTransaction>()
            .Where(x => x.TransactionType == transactionType)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }
}
