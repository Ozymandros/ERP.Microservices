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

    /// <summary>Initializes a new instance of the <see cref="ReservedStockRepository"/> class.</summary>
    /// Initializes a new instance of the ReservedStockRepository class.
    /// <param name="db">The db.</param>
    public ReservedStockRepository(OrdersDbContext db) : base(db)
    {
        _db = db;
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <inheritdoc />
    public async Task<List<ReservedStock>> GetExpiredReservationsAsync()
    {
        return await _db.ReservedStocks
            .Where(r => r.Status == ReservationStatus.Reserved && r.ReservedUntil < DateTime.UtcNow)
            .ToListAsync();
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="orderId">The order Id.</param>
    /// <inheritdoc />
    public async Task<List<ReservedStock>> GetByOrderIdAsync(Guid orderId)
    {
        return await _db.ReservedStocks
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <inheritdoc />
    public async Task<ReservedStock?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _db.ReservedStocks
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
