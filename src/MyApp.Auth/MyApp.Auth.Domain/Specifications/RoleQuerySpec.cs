using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Domain.Specifications;

/// <summary>
/// Specification for querying roles with support for filtering, sorting, and pagination.
/// </summary>
public class RoleQuerySpec : BaseSpecification<ApplicationRole>
{
    public RoleQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<ApplicationRole> Apply(IQueryable<ApplicationRole> query)
    {
        // Apply role-specific filters
        if (Query.Filters?.TryGetValue(nameof(ApplicationRole.Name), out var nameFilter) == true)
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(nameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(ApplicationRole.Description), out var descFilter) == true)
            query = query.Where(r => r.Description != null && r.Description.ToLower().Contains(descFilter.ToString()!.ToLower()));

        // Apply search (searches in name and description)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(term)) ||
                (r.Description != null && r.Description.ToLower().Contains(term))
            );
        }

        return ApplyPaginationAndSorting(query);
    }
}
