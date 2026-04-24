using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Inventory.Domain.Repositories;

public interface IInventoryReservationRepository : IRepository<InventoryReservation, Guid>
{
    Task<List<InventoryReservation>> GetActiveByOrderIdAsync(Guid orderId);
    Task<List<InventoryReservation>> GetExpiredAsync();
}
