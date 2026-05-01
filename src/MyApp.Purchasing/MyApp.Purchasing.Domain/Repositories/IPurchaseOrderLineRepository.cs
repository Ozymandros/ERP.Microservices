using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Purchase Order Line Repository.
/// </summary>
public interface IPurchaseOrderLineRepository : IRepository<PurchaseOrderLine, Guid>
{
    Task<IEnumerable<PurchaseOrderLine>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
