using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Domain.Repositories;

public interface IWarehouseStockRepository : IRepository<WarehouseStock, Guid>
{
    Task<WarehouseStock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId);
    Task<List<WarehouseStock>> GetByProductIdAsync(Guid productId);
    Task<List<WarehouseStock>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<List<WarehouseStock>> GetLowStockAsync(int? reorderLevel = null);
}
