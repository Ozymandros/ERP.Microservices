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

    public override IQueryable<Warehouse> ApplyFilters(IQueryable<Warehouse> query)
    {
        // Apply warehouse-specific filters
        if (Query.Filters?.TryGetValue(nameof(Warehouse.Name), out var nameFilter) == true && !string.IsNullOrEmpty(nameFilter))
            query = query.Where(w => w.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Warehouse.Location), out var locFilter) == true && !string.IsNullOrEmpty(locFilter))
            query = query.Where(w => w.Location.Contains(locFilter, StringComparison.OrdinalIgnoreCase));

        // Apply search (searches in name and location)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(w =>
                w.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                w.Location.Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }

        return query;
    }
}
