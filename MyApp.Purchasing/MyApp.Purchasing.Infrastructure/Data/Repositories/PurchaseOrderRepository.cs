using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

public class PurchaseOrderRepository : Repository<PurchaseOrder, Guid>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Where(x => x.SupplierId == supplierId)
            .Include(x => x.Lines)
            .ToListAsync();
    }

    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Where(x => x.Status == status)
            .Include(x => x.Lines)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetWithLinesAsync(Guid id)
    {
        return await _dbContext.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
