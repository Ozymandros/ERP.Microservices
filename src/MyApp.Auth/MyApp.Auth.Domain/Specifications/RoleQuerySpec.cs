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

    public override IQueryable<ApplicationRole> ApplyFilters(IQueryable<ApplicationRole> query)
    {
        // Apply role-specific filters (case-insensitive key matching)
        var filters = Query.Filters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (filters.TryGetValue(nameof(ApplicationRole.Name), out var nameFilter))
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(nameFilter.ToLower()));

        if (filters.TryGetValue(nameof(ApplicationRole.Description), out var descFilter))
            query = query.Where(r => r.Description != null && r.Description.ToLower().Contains(descFilter.ToLower()));

        // Apply search (searches in name and description)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(term)) ||
                (r.Description != null && r.Description.ToLower().Contains(term))
            );
        }

        return query;
    }
}
