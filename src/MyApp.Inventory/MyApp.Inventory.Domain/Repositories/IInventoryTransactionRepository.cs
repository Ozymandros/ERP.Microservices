using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Inventory Transaction Repository.
/// </summary>
public interface IInventoryTransactionRepository : IRepository<InventoryTransaction, Guid>
{
    /// <summary>Retrieves all inventory transactions associated with a specific product.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>A collection of <see cref="InventoryTransaction"/> entities for the given product.</returns>
    Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId);

    /// <summary>Retrieves all inventory transactions associated with a specific warehouse.</summary>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>A collection of <see cref="InventoryTransaction"/> entities for the given warehouse.</returns>
    Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId);

    /// <summary>Retrieves all inventory transactions of a given transaction type.</summary>
    /// <param name="transactionType">The type of transaction to filter by.</param>
    /// <returns>A collection of matching <see cref="InventoryTransaction"/> entities.</returns>
    Task<IEnumerable<InventoryTransaction>> GetByTransactionTypeAsync(TransactionType transactionType);

    /// <summary>Retrieves an inventory transaction by its reference number.</summary>
    /// <param name="referenceNumber">The external reference number to search for.</param>
    /// <returns>The matching <see cref="InventoryTransaction"/>, or <c>null</c> if not found.</returns>
    Task<InventoryTransaction?> GetByReferenceNumberAsync(string referenceNumber);
}
