using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Domain.Specifications;

/// <summary>
/// Specification for querying warehouses with support for filtering, sorting, and pagination.
/// </summary>
public class WarehouseQuerySpec : BaseSpecification<Warehouse>
{
    public WarehouseQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Warehouse> Apply(IQueryable<Warehouse> query)
    {
        // Apply warehouse-specific filters
        if (Query.Filters?.TryGetValue(nameof(Warehouse.Name), out var nameFilter) == true)
            query = query.Where(w => w.Name.ToLower().Contains(nameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Warehouse.Location), out var locFilter) == true)
            query = query.Where(w => w.Location.ToLower().Contains(locFilter.ToString()!.ToLower()));

        // Apply search (searches in name and location)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(term) ||
                w.Location.ToLower().Contains(term)
            );
        }

        return ApplyPaginationAndSorting(query);
    }
}
