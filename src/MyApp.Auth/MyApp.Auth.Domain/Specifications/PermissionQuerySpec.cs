using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Domain.Specifications;

/// <summary>
/// Specification for querying permissions with support for filtering, sorting, and pagination.
/// </summary>
public class PermissionQuerySpec : BaseSpecification<Permission>
{
    public PermissionQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Permission> ApplyFilters(IQueryable<Permission> query)
    {
        // Apply permission-specific filters (case-insensitive key matching)
        var filters = Query.Filters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (filters.TryGetValue(nameof(Permission.Module), out var moduleFilter) && !string.IsNullOrEmpty(moduleFilter))
            query = query.Where(p => p.Module.Contains(moduleFilter, StringComparison.OrdinalIgnoreCase));

        if (filters.TryGetValue(nameof(Permission.Action), out var actionFilter) && !string.IsNullOrEmpty(actionFilter))
            query = query.Where(p => p.Action.Contains(actionFilter, StringComparison.OrdinalIgnoreCase));

        if (filters.TryGetValue(nameof(Permission.Description), out var descFilter) && !string.IsNullOrEmpty(descFilter))
            query = query.Where(p => p.Description != null && p.Description.Contains(descFilter, StringComparison.OrdinalIgnoreCase));

        // Apply search (searches in module, action, and description)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(p =>
                p.Module.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        return query;
    }
}
