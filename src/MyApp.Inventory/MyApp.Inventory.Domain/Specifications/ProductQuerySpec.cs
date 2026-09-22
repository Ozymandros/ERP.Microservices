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

    /// <summary>Initialises a new instance of <see cref="ProductQuerySpec"/> with the supplied query parameters.</summary>
    /// Initializes a new instance of the ProductQuerySpec class.
    /// <param name="query">The query.</param>
    public ProductQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <summary>Applies product-specific filters (SKU, name, price range) and the search term to the query.</summary>
    /// Applies query filters to the specification.
    /// <param name="query">The query.</param>
    /// <returns>The filtered <see cref="IQueryable{Product}"/>.</returns>
    public override IQueryable<Product> ApplyFilters(IQueryable<Product> query)
    {
        // Apply product-specific filters
        if (Query.Filters?.TryGetValue(nameof(Product.SKU), out var skuFilter) == true && !string.IsNullOrEmpty(skuFilter))
            query = query.Where(p => p.SKU.Contains(skuFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Product.Name), out var nameFilter) == true && !string.IsNullOrEmpty(nameFilter))
            query = query.Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

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
            var term = Query.SearchTerm;
            query = query.Where(p =>
                p.SKU.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        return query;
    }
}
