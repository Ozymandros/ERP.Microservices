using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder, Guid>
{
    Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status);
    Task<PurchaseOrder?> GetWithLinesAsync(Guid id);
}
