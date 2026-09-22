using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="OrderLine"/> entities.</summary>
public class OrderLineRepository : Repository<OrderLine, Guid>, IOrderLineRepository
{
    /// <summary>Initializes a new instance of the <see cref="OrderLineRepository"/> class.</summary>
    /// Initializes a new instance of the OrderLineRepository class.
    /// <param name="db">The db.</param>
    public OrderLineRepository(OrdersDbContext db) : base(db)
    {
    }
}
