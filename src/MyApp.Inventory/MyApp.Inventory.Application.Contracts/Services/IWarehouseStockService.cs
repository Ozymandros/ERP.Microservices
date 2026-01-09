using MyApp.Inventory.Application.Contracts.DTOs;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IWarehouseStockService
{
    Task<WarehouseStockDto?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId);
    Task<List<WarehouseStockDto>> GetByProductIdAsync(Guid productId);
    Task<List<WarehouseStockDto>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<StockAvailabilityDto?> GetProductAvailabilityAsync(Guid productId);
    
    Task<ReservationDto> ReserveStockAsync(ReserveStockDto dto);
    Task ReleaseReservationAsync(Guid reservationId);
    
    Task TransferStockAsync(StockTransferDto dto);
    Task AdjustStockAsync(StockAdjustmentDto dto);
    
    Task<List<WarehouseStockDto>> GetLowStockAsync();
}
