using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>
/// Provides Order Repository functionality.
/// </summary>
public class OrderRepository : DbContextRepositoryBase, IOrderRepository
{
    private readonly OrdersDbContext _db;

    public OrderRepository(OrdersDbContext db) : base(db)
    {
        _db = db;
    }

    /// <summary>Add Async.</summary>
    public async Task AddAsync(Order entity)
    {
        await _db.Orders.AddAsync(entity);
        await base.SaveChangesAsync();
    }

    /// <summary>Delete Async.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var existing = await _db.Orders.FindAsync(id);
        if (existing != null)
        {
            _db.Orders.Remove(existing);
            await base.SaveChangesAsync();
        }
    }

    /// <summary>Get By Id Async.</summary>
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>Get By Order Number Async.</summary>
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    /// <summary>List Async.</summary>
    public async Task<IEnumerable<Order>> ListAsync()
    {
        return await _db.Orders.Include(o => o.Lines).ToListAsync();
    }

    /// <summary>Update Async.</summary>
    public async Task UpdateAsync(Order entity)
    {
        _db.Orders.Update(entity);
        await base.SaveChangesAsync();
    }

    /// <summary>Query Async.</summary>
    public async Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec)
    {
        var baseQuery = _db.Orders.Include(o => o.Lines).AsQueryable();
        var totalCount = await baseQuery.CountAsync();
        var paginatedQuery = spec.Apply(baseQuery);
        var items = await paginatedQuery.ToListAsync();
        return new PaginatedResult<Order>(
            items,
            (spec as BaseSpecification<Order>)?.Query.Page ?? 1,
            (spec as BaseSpecification<Order>)?.Query.PageSize ?? items.Count,
            totalCount);
    }
}
