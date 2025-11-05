using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

public class PurchaseOrderLineRepository : Repository<PurchaseOrderLine, Guid>, IPurchaseOrderLineRepository
{
    public PurchaseOrderLineRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<PurchaseOrderLine>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        return await _dbContext.Set<PurchaseOrderLine>()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
    }
}
