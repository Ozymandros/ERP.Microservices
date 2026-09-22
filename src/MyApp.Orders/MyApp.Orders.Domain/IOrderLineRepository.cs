using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Domain;

/// <summary>Repository interface for OrderLine entities.</summary>
public interface IOrderLineRepository : IRepository<OrderLine, Guid>
{
}
