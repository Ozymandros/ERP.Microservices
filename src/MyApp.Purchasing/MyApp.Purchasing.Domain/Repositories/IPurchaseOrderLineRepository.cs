using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

public interface IPurchaseOrderLineRepository : IRepository<PurchaseOrderLine, Guid>
{
    Task<IEnumerable<PurchaseOrderLine>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
