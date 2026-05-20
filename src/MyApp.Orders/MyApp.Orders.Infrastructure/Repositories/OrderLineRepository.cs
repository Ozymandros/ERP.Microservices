using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>
/// Provides Order Line Repository functionality.
/// </summary>
public class OrderLineRepository : DbContextRepositoryBase, IOrderLineRepository
{
    private readonly OrdersDbContext _db;

    public OrderLineRepository(OrdersDbContext db) : base(db)
    {
        _db = db;
    }

    /// <summary>Add Async.</summary>
    public async Task AddAsync(OrderLine entity)
    {
        await _db.OrderLines.AddAsync(entity);
        await base.SaveChangesAsync();
    }

    /// <summary>Delete Async.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var existing = await _db.OrderLines.FindAsync(id);
        if (existing != null)
        {
            _db.OrderLines.Remove(existing);
            await base.SaveChangesAsync();
        }
    }

    /// <summary>Get By Id Async.</summary>
    public async Task<OrderLine?> GetByIdAsync(Guid id)
    {
        return await _db.OrderLines.FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<OrderLine>> ListAsync()
    {
        return await _db.OrderLines.ToListAsync();
    }

    /// <summary>Update Async.</summary>
    public async Task UpdateAsync(OrderLine entity)
    {
        _db.OrderLines.Update(entity);
        await base.SaveChangesAsync();
    }
}
