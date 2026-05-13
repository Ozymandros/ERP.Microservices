using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Inventory Transaction Repository.
/// </summary>
public interface IInventoryTransactionRepository : IRepository<InventoryTransaction, Guid>
{
    Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<IEnumerable<InventoryTransaction>> GetByTransactionTypeAsync(TransactionType transactionType);
    Task<InventoryTransaction?> GetByReferenceNumberAsync(string referenceNumber);
}
