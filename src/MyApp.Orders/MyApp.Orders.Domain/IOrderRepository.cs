using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Domain;

/// <summary>Repository interface for Order entities.</summary>
public interface IOrderRepository : IRepository<Order, Guid>
{
    /// <summary>Retrieves an order by its order number.</summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}
