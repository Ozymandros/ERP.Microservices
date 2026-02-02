using System;
using System.Threading.Tasks;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Orders.Domain.Entities;

namespace MyApp.Orders.Domain
{
    public interface IOrderRepository : IRepository<Entities.Order, Guid>
    {
        Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec);
        // Additional domain-specific methods can be added here
    }
}
