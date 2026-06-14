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

    public EntityChangeRepository(AuditSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<EntityChange?> GetByIdWithPropertiesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EntityChanges
            .AsNoTracking()
            .Include(e => e.PropertyChanges)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

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
