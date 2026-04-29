using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Inventory.Infrastructure.Repositories;

/// <summary>
/// Provides Inventory Reservation Repository functionality.
/// </summary>
public class InventoryReservationRepository : Repository<InventoryReservation, Guid>, IInventoryReservationRepository
{
    /// <summary>base.</summary>
    public InventoryReservationRepository(InventoryDbContext context) : base(context)
    {
    }

    /// <summary>Get Active By Order Id Async.</summary>
    public async Task<List<InventoryReservation>> GetActiveByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.Set<InventoryReservation>()
            .Where(r => r.OrderId == orderId && r.Status == InventoryReservationStatus.Reserved)
            .ToListAsync();
    }

    /// <summary>Get Expired Async.</summary>
    public async Task<List<InventoryReservation>> GetExpiredAsync()
    {
        return await _dbContext.Set<InventoryReservation>()
            .Where(r => r.Status == InventoryReservationStatus.Reserved && r.ReservedUntil < DateTime.UtcNow)
            .ToListAsync();
    }
}
