using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Warehouse Stock Repository.
/// </summary>
public interface IWarehouseStockRepository : IRepository<WarehouseStock, Guid>
{
    /// <summary>Retrieves the stock record for a specific product in a specific warehouse.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>The matching <see cref="WarehouseStock"/>, or <c>null</c> if not found.</returns>
    Task<WarehouseStock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId);

    /// <summary>Retrieves all warehouse stock records for a given product across all warehouses.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>A list of <see cref="WarehouseStock"/> records for the product.</returns>
    Task<List<WarehouseStock>> GetByProductIdAsync(Guid productId);

    /// <summary>Retrieves all stock records held in a specific warehouse.</summary>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>A list of <see cref="WarehouseStock"/> records for the warehouse.</returns>
    Task<List<WarehouseStock>> GetByWarehouseIdAsync(Guid warehouseId);

    /// <summary>Retrieves warehouse stock records whose available quantity is at or below the reorder level.</summary>
    /// <param name="reorderLevel">Optional override for the reorder threshold; defaults to each product's own reorder level when <c>null</c>.</param>
    /// <returns>A list of low-stock <see cref="WarehouseStock"/> records.</returns>
    Task<List<WarehouseStock>> GetLowStockAsync(int? reorderLevel = null);
}
