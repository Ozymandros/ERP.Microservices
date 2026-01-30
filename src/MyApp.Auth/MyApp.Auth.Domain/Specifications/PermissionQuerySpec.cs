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
        
        if (filters.TryGetValue(nameof(Permission.Module), out var moduleFilter))
            query = query.Where(p => p.Module.ToLower().Contains(moduleFilter.ToLower()));

        if (filters.TryGetValue(nameof(Permission.Action), out var actionFilter))
            query = query.Where(p => p.Action.ToLower().Contains(actionFilter.ToLower()));

        if (filters.TryGetValue(nameof(Permission.Description), out var descFilter))
            query = query.Where(p => p.Description != null && p.Description.ToLower().Contains(descFilter.ToLower()));

        // Apply search (searches in module, action, and description)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Module.ToLower().Contains(term) ||
                p.Action.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term))
            );
        }

        return query;
    }
}
