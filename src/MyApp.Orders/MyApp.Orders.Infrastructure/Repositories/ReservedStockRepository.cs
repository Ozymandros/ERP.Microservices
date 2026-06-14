using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="ReservedStock"/> entities.</summary>
public class ReservedStockRepository : Repository<ReservedStock, Guid>, IReservedStockRepository
{
    private readonly OrdersDbContext _db;

    public ReservedStockRepository(OrdersDbContext db) : base(db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<ReservedStock>> GetExpiredReservationsAsync()
    {
        return await _db.ReservedStocks
            .Where(r => r.Status == ReservationStatus.Reserved && r.ReservedUntil < DateTime.UtcNow)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ReservedStock>> GetByOrderIdAsync(Guid orderId)
    {
        return await _db.ReservedStocks
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ReservedStock?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _db.ReservedStocks
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
