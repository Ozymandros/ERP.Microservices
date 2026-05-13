using System;
using System.Threading.Tasks;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Domain
{
/// <summary>Repository interface for Order entities.</summary>
public interface IOrderRepository : IRepository<Entities.Order, Guid>
{
    /// <summary>Queries orders based on a specification with pagination support.</summary>
    Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec);
    /// <summary>Gets an order by its order number.</summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}
}
