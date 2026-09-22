using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Orders.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="Order"/> aggregates.</summary>
public class OrderRepository : Repository<Order, Guid>, IOrderRepository
{
    private readonly OrdersDbContext _db;

    /// <summary>Initializes a new instance of the <see cref="OrderRepository"/> class.</summary>
    /// Initializes a new instance of the OrderRepository class.
    /// <param name="db">The db.</param>
    public OrderRepository(OrdersDbContext db) : base(db)
    {
        _db = db;
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <inheritdoc />
    public override async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <inheritdoc />
    public override async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _db.Orders.Include(o => o.Lines).ToListAsync();
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <inheritdoc />
    public override async Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

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
