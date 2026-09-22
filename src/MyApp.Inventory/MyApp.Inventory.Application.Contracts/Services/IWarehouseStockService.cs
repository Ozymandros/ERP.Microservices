using MyApp.Inventory.Application.Contracts.DTOs;

namespace MyApp.Inventory.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Warehouse Stock Service.
/// </summary>
public interface IWarehouseStockService
{
    /// <summary>Retrieves the stock record for a specific product in a specific warehouse.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>The matching <see cref="WarehouseStockDto"/>, or <c>null</c> if not found.</returns>
    Task<WarehouseStockDto?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId);

    /// <summary>Retrieves all warehouse stock records for a given product across all warehouses.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>A list of <see cref="WarehouseStockDto"/> records for the product.</returns>
    Task<List<WarehouseStockDto>> GetByProductIdAsync(Guid productId);

    /// <summary>Retrieves all stock records held in a specific warehouse.</summary>
    /// <param name="warehouseId">The unique identifier of the warehouse.</param>
    /// <returns>A list of <see cref="WarehouseStockDto"/> records for the warehouse.</returns>
    Task<List<WarehouseStockDto>> GetByWarehouseIdAsync(Guid warehouseId);

    /// <summary>Retrieves the aggregated stock availability for a product across all warehouses.</summary>
    /// <param name="productId">The unique identifier of the product.</param>
    /// <returns>A <see cref="StockAvailabilityDto"/> summarising availability, or <c>null</c> if the product is not found.</returns>
    Task<StockAvailabilityDto?> GetProductAvailabilityAsync(Guid productId);

    /// <summary>Reserves stock for a specific product in a warehouse on behalf of an order.</summary>
    /// <param name="dto">The reservation request data.</param>
    /// <returns>A <see cref="ReservationDto"/> describing the created reservation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no stock record exists for the product/warehouse combination.</exception>
    Task<ReservationDto> ReserveStockAsync(ReserveStockDto dto);

    /// <summary>Releases a previously created stock reservation and returns the quantity to available stock.</summary>
    /// <param name="reservationId">The unique identifier of the reservation to release.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no reservation with the given identifier is found.</exception>
    Task ReleaseReservationAsync(Guid reservationId);

    /// <summary>Transfers stock of a product from one warehouse to another.</summary>
    /// <param name="dto">The transfer request data including source warehouse, destination warehouse and quantity.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source warehouse has insufficient available stock.</exception>
    Task TransferStockAsync(StockTransferDto dto);

    /// <summary>Applies a manual stock adjustment to a product in a specific warehouse.</summary>
    /// <param name="dto">The adjustment data including the product, warehouse and quantity change.</param>
    /// <exception cref="InvalidOperationException">Thrown when the adjustment would result in negative available stock.</exception>
    Task AdjustStockAsync(StockAdjustmentDto dto);

    /// <summary>Retrieves all warehouse stock records whose available quantity is at or below the product's reorder level.</summary>
    /// <returns>A list of low-stock <see cref="WarehouseStockDto"/> records.</returns>
    Task<List<WarehouseStockDto>> GetLowStockAsync();

    /// <summary>Retrieves all warehouse stock records.</summary>
    /// <returns>A list of all <see cref="WarehouseStockDto"/> records.</returns>
    Task<List<WarehouseStockDto>> GetAllWarehouseStocksAsync();
}
