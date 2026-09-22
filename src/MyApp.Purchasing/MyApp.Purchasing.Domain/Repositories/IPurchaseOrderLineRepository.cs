using MyApp.Shared.Domain.Repositories;
using MyApp.Purchasing.Domain.Entities;

namespace MyApp.Purchasing.Domain.Repositories;

/// <summary>
/// Defines the contract for I Purchase Order Line Repository.
/// </summary>
public interface IPurchaseOrderLineRepository : IRepository<PurchaseOrderLine, Guid>
{
    /// <summary>Returns all lines belonging to the specified purchase order.</summary>
    /// <param name="purchaseOrderId">The unique identifier of the purchase order.</param>
    /// <returns>A collection of order lines for the purchase order.</returns>
    Task<IEnumerable<PurchaseOrderLine>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
