using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Inventory.Infrastructure.Repositories;

public class WarehouseStockRepository : Repository<WarehouseStock, Guid>, IWarehouseStockRepository
{
    public WarehouseStockRepository(InventoryDbContext context) : base(context)
    {
    }

    public async Task<WarehouseStock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId)
    {
        return await _dbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .FirstOrDefaultAsync(ws => ws.ProductId == productId && ws.WarehouseId == warehouseId);
    }

    public async Task<List<WarehouseStock>> GetByProductIdAsync(Guid productId)
    {
        return await _dbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.ProductId == productId)
            .ToListAsync();
    }

    public async Task<List<WarehouseStock>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        return await _dbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.WarehouseId == warehouseId)
            .ToListAsync();
    }

    public async Task<List<WarehouseStock>> GetLowStockAsync(int? reorderLevel = null)
    {
        var query = _dbContext.Set<WarehouseStock>()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .AsQueryable();

        if (reorderLevel.HasValue)
        {
            query = query.Where(ws => ws.AvailableQuantity <= reorderLevel.Value);
        }
        else
        {
            query = query.Where(ws => ws.AvailableQuantity <= ws.Product.ReorderLevel);
        }

        return await query.ToListAsync();
    }
}
