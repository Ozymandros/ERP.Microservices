using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Orders.Domain.Repositories;

/// <summary>Repository interface for ReservedStock entities.</summary>
public interface IReservedStockRepository : IRepository<ReservedStock, Guid>
{
    /// <summary>Retrieves all expired reservations.</summary>
    Task<List<ReservedStock>> GetExpiredReservationsAsync();
    /// <summary>Retrieves all reservations associated with a specific order.</summary>
    Task<List<ReservedStock>> GetByOrderIdAsync(Guid orderId);
    /// <summary>Retrieves a reservation with its related details by ID.</summary>
    Task<ReservedStock?> GetByIdWithDetailsAsync(Guid id);
}
