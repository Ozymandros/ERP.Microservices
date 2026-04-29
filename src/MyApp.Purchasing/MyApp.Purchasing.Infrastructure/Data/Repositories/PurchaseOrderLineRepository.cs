using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;

namespace MyApp.Purchasing.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Purchase Order Line Repository functionality.
/// </summary>
public class PurchaseOrderLineRepository : Repository<PurchaseOrderLine, Guid>, IPurchaseOrderLineRepository
{
    /// <summary>base.</summary>
    public PurchaseOrderLineRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Purchase Order Id Async.</summary>
    public async Task<IEnumerable<PurchaseOrderLine>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        return await _dbContext.Set<PurchaseOrderLine>()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
    }
}
