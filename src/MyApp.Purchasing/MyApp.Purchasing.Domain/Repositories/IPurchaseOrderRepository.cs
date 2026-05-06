using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Purchase Order Repository.
/// </summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder, Guid>
{
    Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status);
    Task<PurchaseOrder?> GetWithLinesAsync(Guid id);
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber);
}
