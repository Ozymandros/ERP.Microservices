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
    /// <param name="dbContext">The db Context.</param>
    public PurchaseOrderRepository(PurchasingDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Suppliers Id Async.</summary>
    /// <param name="supplierId">The supplier Id.</param>
    public async Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PurchaseOrder>()
            .Where(x => x.SupplierId == supplierId)
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Get By Status Async.</summary>
    /// <param name="status">The status.</param>
    public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PurchaseOrder>()
            .Where(x => x.Status == status)
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Get With Lines Async.</summary>
    /// <param name="id">The id.</param>
    public async Task<PurchaseOrder?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets the order number asynchronously.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
    }
}
