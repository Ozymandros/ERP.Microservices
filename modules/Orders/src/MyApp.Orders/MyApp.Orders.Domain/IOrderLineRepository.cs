using System;

namespace MyApp.Orders.Domain
{
    public interface IOrderLineRepository : IRepository<Entities.OrderLine, Guid>
    {
    }
}
