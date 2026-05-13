using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Purchase Order Repository functionality.
/// </summary>
public class PurchaseOrderRepository : Repository<PurchaseOrder, Guid>, IPurchaseOrderRepository
{
    /// <summary>base.</summary>
    public PurchaseOrderRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Suppliers Id Async.</summary>
    public async Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Where(x => x.SupplierId == supplierId)
            .Include(x => x.Lines)
            .ToListAsync();
    }

    /// <summary>Get By Status Async.</summary>
    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Where(x => x.Status == status)
            .Include(x => x.Lines)
            .ToListAsync();
    }

    /// <summary>Get With Lines Async.</summary>
    public async Task<PurchaseOrder?> GetWithLinesAsync(Guid id)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
    }
}
