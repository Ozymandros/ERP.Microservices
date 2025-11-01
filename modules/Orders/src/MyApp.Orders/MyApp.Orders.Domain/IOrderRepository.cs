using System;

namespace MyApp.Orders.Domain
{
    public interface IOrderRepository : IRepository<Entities.Order, Guid>
    {
        // Additional domain-specific methods can be added here
    }
}
