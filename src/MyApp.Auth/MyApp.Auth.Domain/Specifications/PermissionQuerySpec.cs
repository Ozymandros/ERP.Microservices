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

    public override IQueryable<Permission> Apply(IQueryable<Permission> query)
    {
        // Apply permission-specific filters
        if (Query.Filters?.TryGetValue(nameof(Permission.Module), out var moduleFilter) == true)
            query = query.Where(p => p.Module.ToLower().Contains(moduleFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Permission.Action), out var actionFilter) == true)
            query = query.Where(p => p.Action.ToLower().Contains(actionFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Permission.Description), out var descFilter) == true)
            query = query.Where(p => p.Description != null && p.Description.ToLower().Contains(descFilter.ToString()!.ToLower()));

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

        return ApplyPaginationAndSorting(query);
    }
}
