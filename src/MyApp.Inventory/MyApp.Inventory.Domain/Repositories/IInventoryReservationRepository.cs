using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Inventory Reservation Repository.
/// </summary>
public interface IInventoryReservationRepository : IRepository<InventoryReservation, Guid>
{
    Task<List<InventoryReservation>> GetActiveByOrderIdAsync(Guid orderId);
    Task<List<InventoryReservation>> GetExpiredAsync();
}
