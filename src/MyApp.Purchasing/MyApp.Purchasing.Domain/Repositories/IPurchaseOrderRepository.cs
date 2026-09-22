using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Purchase Order Repository.
/// </summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder, Guid>
{
    /// <summary>Returns all purchase orders placed with the specified supplier.</summary>
    /// <param name="supplierId">The unique identifier of the supplier.</param>
    /// <returns>A collection of purchase orders for the supplier.</returns>
    Task<IEnumerable<PurchaseOrder>> GetBySuppliersIdAsync(Guid supplierId);
    /// <summary>Returns all purchase orders with the specified status.</summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A collection of purchase orders matching the status.</returns>
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status);
    /// <summary>Returns a purchase order including its lines for the specified ID.</summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <returns>The purchase order with lines loaded, or <c>null</c> if not found.</returns>
    Task<PurchaseOrder?> GetWithLinesAsync(Guid id);
    /// <summary>Returns the purchase order with the specified order number.</summary>
    /// <param name="orderNumber">The order number to look up.</param>
    /// <returns>The matching purchase order, or <c>null</c> if not found.</returns>
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber);
}
