using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Domain;

/// <summary>Repository interface for Order entities.</summary>
public interface IOrderRepository : IRepository<Order, Guid>
{
    /// <summary>Retrieves an order by its order number.</summary>
    /// <param name="orderNumber">The order number.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
}
