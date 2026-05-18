using MyApp.Shared.Domain.Extensions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Audit.Domain.Specifications;

/// <summary>Specification for querying entity changes with filtering, sorting, and pagination.</summary>
public class EntityChangeQuerySpec : BaseSpecification<EntityChange>
{
    public EntityChangeQuerySpec(QuerySpec query) : base(query) { }

    public override IQueryable<EntityChange> ApplyFilters(IQueryable<EntityChange> query)
    {
        if (Query.Filters?.TryGetValue(nameof(EntityChange.EntityName), out var entityNameFilter) == true
            && !string.IsNullOrWhiteSpace(entityNameFilter))
        {
            var term = entityNameFilter.Trim();
            query = query.Where(e => e.EntityName.Contains(term));
        }

        if (Query.Filters?.TryGetValue(nameof(EntityChange.EntityId), out var entityIdFilter) == true
            && Guid.TryParse(entityIdFilter, out var entityId))
        {
            query = query.Where(e => e.EntityId == entityId);
        }

        if (Query.Filters?.TryGetValue(nameof(EntityChange.ChangeType), out var changeTypeFilter) == true
            && Enum.TryParse<ChangeTypeEnum>(changeTypeFilter, true, out var changeType))
        {
            query = query.Where(e => e.ChangeType == changeType);
        }

        if (Query.Filters?.TryGetValue("CreatedAtFrom", out var createdAtFromFilter) == true
            && DateTime.TryParse(createdAtFromFilter, out var createdAtFrom))
        {
            query = query.Where(e => e.CreatedAt >= createdAtFrom);
        }

        if (Query.Filters?.TryGetValue("CreatedAtTo", out var createdAtToFilter) == true
            && DateTime.TryParse(createdAtToFilter, out var createdAtTo))
        {
            query = query.Where(e => e.CreatedAt <= createdAtTo);
        }

        if (Query.Filters?.TryGetValue(nameof(EntityChange.CreatedBy), out var createdByFilter) == true
            && !string.IsNullOrWhiteSpace(createdByFilter))
        {
            var createdBy = createdByFilter.Trim();
            query = query.Where(e => e.CreatedBy.Contains(createdBy));
        }

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var searchTerm = Query.SearchTerm.Trim();
            query = query.Where(e =>
                e.EntityName.Contains(searchTerm) ||
                e.CreatedBy.Contains(searchTerm) ||
                e.ChangeType.ToString().Contains(searchTerm));
        }

        return query;
    }

    public override IQueryable<EntityChange> Apply(IQueryable<EntityChange> query)
    {
        query = ApplyFilters(query);

        query = string.IsNullOrEmpty(Query.SortBy)
            ? query.OrderByDescending(e => e.CreatedAt)
            : query.OrderByDynamic(Query.SortBy, Query.SortDesc);

        var skip = (Query.Page - 1) * Query.PageSize;
        return query.Skip(skip).Take(Query.PageSize);
    }
}
