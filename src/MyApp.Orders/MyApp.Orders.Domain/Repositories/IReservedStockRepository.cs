using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Domain.Repositories;

public interface IReservedStockRepository : IRepository<ReservedStock, Guid>
{
    Task<List<ReservedStock>> GetExpiredReservationsAsync();
    Task<List<ReservedStock>> GetByOrderIdAsync(Guid orderId);
    Task<ReservedStock?> GetByIdWithDetailsAsync(Guid id);
}
