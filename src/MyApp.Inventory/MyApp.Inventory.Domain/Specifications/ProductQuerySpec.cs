using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Domain.Specifications;

/// <summary>
/// Specification for querying products with support for filtering, sorting, and pagination.
/// </summary>
public class ProductQuerySpec : BaseSpecification<Product>
{
    private const string MinPriceFilterKey = $"{nameof(Product.UnitPrice)}Min";
    private const string MaxPriceFilterKey = $"{nameof(Product.UnitPrice)}Max";

    public ProductQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Product> Apply(IQueryable<Product> query)
    {
        // Apply product-specific filters
        if (Query.Filters?.TryGetValue(nameof(Product.SKU), out var skuFilter) == true)
            query = query.Where(p => p.SKU.ToLower().Contains(skuFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(Product.Name), out var nameFilter) == true)
            query = query.Where(p => p.Name.ToLower().Contains(nameFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(MinPriceFilterKey, out var minPrice) == true)
        {
            if (decimal.TryParse(minPrice.ToString(), out var price))
                query = query.Where(p => p.UnitPrice >= price);
        }

        if (Query.Filters?.TryGetValue(MaxPriceFilterKey, out var maxPrice) == true)
        {
            if (decimal.TryParse(maxPrice.ToString(), out var price))
                query = query.Where(p => p.UnitPrice <= price);
        }

        // Apply search (searches in SKU, Name, and Description)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(p =>
                p.SKU.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term))
            );
        }

        return ApplyPaginationAndSorting(query);
    }
}
