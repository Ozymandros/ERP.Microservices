using System;

namespace MyApp.Orders.Domain
{
    /// <summary>Repository interface for OrderLine entities.</summary>
    public interface IOrderLineRepository : IRepository<Entities.OrderLine, Guid>
    {
    }
}
