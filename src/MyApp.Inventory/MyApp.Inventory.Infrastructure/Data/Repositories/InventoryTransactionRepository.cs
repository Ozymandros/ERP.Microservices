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
    /// <param name="dbContext">The db Context.</param>
    public InventoryTransactionRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Product Id Async.</summary>
    /// <param name="productId">The product Id.</param>
    public async Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId)
    {
        return await DbContext.Set<InventoryTransaction>()
            .Where(x => x.ProductId == productId)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }

    /// <summary>Get By Warehouse Id Async.</summary>
    /// <param name="warehouseId">The warehouse Id.</param>
    public async Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        return await DbContext.Set<InventoryTransaction>()
            .Where(x => x.WarehouseId == warehouseId)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }

    /// <summary>Get By Transaction Type Async.</summary>
    /// <param name="transactionType">The transaction Type.</param>
    public async Task<IEnumerable<InventoryTransaction>> GetByTransactionTypeAsync(TransactionType transactionType)
    {
        return await DbContext.Set<InventoryTransaction>()
            .Where(x => x.TransactionType == transactionType)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .ToListAsync();
    }

    /// <summary>Retrieves an inventory transaction by its external reference number.</summary>
    /// Gets the reference number asynchronously.
    /// <param name="referenceNumber">The reference Number.</param>
    /// <returns>The matching <see cref="InventoryTransaction"/>, or <c>null</c> if not found.</returns>
    public async Task<InventoryTransaction?> GetByReferenceNumberAsync(string referenceNumber)
    {
        return await DbContext.Set<InventoryTransaction>()
            .Where(x => x.ReferenceNumber == referenceNumber)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync();
    }
}
