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
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <inheritdoc />
    public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <inheritdoc />
    public override async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Orders.Include(o => o.Lines).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the operation.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <inheritdoc />
    public override async Task<PaginatedResult<Order>> QueryAsync(ISpecification<Order> spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var baseQuery = _db.Orders.Include(o => o.Lines).AsQueryable();
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var paginatedQuery = spec.Apply(baseQuery);
        var items = await paginatedQuery.ToListAsync(cancellationToken);
        return new PaginatedResult<Order>(
            items,
            (spec as BaseSpecification<Order>)?.Query.Page ?? 1,
            (spec as BaseSpecification<Order>)?.Query.PageSize ?? items.Count,
            totalCount);
    }
}
