using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Inventory Reservation Repository.
/// </summary>
public interface IInventoryReservationRepository : IRepository<InventoryReservation, Guid>
{
    /// <summary>Retrieves all active (not yet released or expired) reservations for a given order.</summary>
    /// <param name="orderId">The unique identifier of the order.</param>
    /// <returns>A list of active <see cref="InventoryReservation"/> entities for the given order.</returns>
    Task<List<InventoryReservation>> GetActiveByOrderIdAsync(Guid orderId);

    /// <summary>Retrieves all reservations whose expiry time has passed and status is still Reserved.</summary>
    /// <returns>A list of expired <see cref="InventoryReservation"/> entities.</returns>
    Task<List<InventoryReservation>> GetExpiredAsync();
}
