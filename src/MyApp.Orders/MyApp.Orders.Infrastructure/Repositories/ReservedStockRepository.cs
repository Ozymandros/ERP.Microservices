using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Orders.Infrastructure.Data;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>
/// Provides Reserved Stock Repository functionality.
/// </summary>
public class ReservedStockRepository : IReservedStockRepository
{
    private readonly OrdersDbContext _db;

    public ReservedStockRepository(OrdersDbContext db)
    {
        _db = db;
    }

    // ── IRepository<ReservedStock, Guid> (local Orders contract) ─────────────

    /// <summary>Get By Id Async.</summary>
    public async Task<ReservedStock?> GetByIdAsync(Guid id)
    {
        return await _db.ReservedStocks.FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<ReservedStock>> ListAsync()
    {
        return await _db.ReservedStocks.ToListAsync();
    }

    /// <summary>Add Async.</summary>
    public async Task AddAsync(ReservedStock entity)
    {
        await _db.ReservedStocks.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    /// <summary>Update Async.</summary>
    public async Task UpdateAsync(ReservedStock entity)
    {
        _db.ReservedStocks.Update(entity);
        await _db.SaveChangesAsync();
    }

    /// <summary>Delete Async.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var existing = await _db.ReservedStocks.FindAsync(id);
        if (existing != null)
        {
            _db.ReservedStocks.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    // ── IReservedStockRepository domain-specific queries ─────────────────────

    /// <summary>Get Expired Reservations Async.</summary>
    public async Task<List<ReservedStock>> GetExpiredReservationsAsync()
    {
        return await _db.ReservedStocks
            .Where(r => r.Status == ReservationStatus.Reserved && r.ReservedUntil < DateTime.UtcNow)
            .ToListAsync();
    }

    /// <summary>Get By Order Id Async.</summary>
    public async Task<List<ReservedStock>> GetByOrderIdAsync(Guid orderId)
    {
        return await _db.ReservedStocks
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
    }

    /// <summary>Get By Id With Details Async.</summary>
    public async Task<ReservedStock?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _db.ReservedStocks
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
