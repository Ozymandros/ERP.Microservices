using Microsoft.EntityFrameworkCore;
using MyApp.Audit.Domain;
using MyApp.Audit.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Audit.Infrastructure.Repositories;

/// <summary>EF Core repository for entity change audit records.</summary>
public class EntityChangeRepository : Repository<EntityChange, Guid>, IEntityChangeRepository
{
    private readonly AuditSqlDbContext _context;

    /// <summary>
    /// Initializes a new instance of the EntityChangeRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public EntityChangeRepository(AuditSqlDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the id with properties asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The matching <see cref="EntityChange"/> with property changes included, or <see langword="null"/> if not found.</returns>
    public async Task<EntityChange?> GetByIdWithPropertiesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EntityChanges
            .AsNoTracking()
            .Include(e => e.PropertyChanges)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets the entity asynchronously.
    /// with property changes eagerly loaded.
    /// </summary>
    /// <param name="entityName">The entity Name.</param>
    /// <param name="entityId">The entity Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A list of entity change records for the specified entity.</returns>
    public async Task<List<EntityChange>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EntityChanges
            .AsNoTracking()
            .Include(e => e.PropertyChanges)
            .Where(e => e.EntityName == entityName && e.EntityId == entityId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Query asynchronously.
    /// filters and pagination while eagerly loading property changes.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <returns>A paginated result containing the matching entity change records.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="spec"/> is <see langword="null"/>.</exception>
    public override async Task<PaginatedResult<EntityChange>> QueryAsync(ISpecification<EntityChange> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var baseQuery = _context.EntityChanges
            .AsNoTracking()
            .Include(e => e.PropertyChanges)
            .AsQueryable();

        var filteredQuery = spec.ApplyFilters(baseQuery);
        var totalCount = await filteredQuery.CountAsync();

        var finalQuery = spec.Apply(baseQuery);
        var items = await finalQuery.ToListAsync();

        int pageNumber = 1;
        int pageSize = items.Count;

        if (spec is BaseSpecification<EntityChange> baseSpec)
        {
            pageNumber = baseSpec.Query.Page;
            pageSize = baseSpec.Query.PageSize;
        }

        return new PaginatedResult<EntityChange>(items, pageNumber, pageSize, totalCount);
    }
}
