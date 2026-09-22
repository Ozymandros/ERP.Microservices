using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Domain.Specifications;

/// <summary>
/// Specification for querying inventory transactions with support for filtering, sorting, and pagination.
/// </summary>
public class InventoryTransactionQuerySpec : BaseSpecification<InventoryTransaction>
{
    private const string MinQuantityFilterKey = $"{nameof(InventoryTransaction.QuantityChange)}Min";
    private const string MaxQuantityFilterKey = $"{nameof(InventoryTransaction.QuantityChange)}Max";

    /// <summary>Initialises a new instance of <see cref="InventoryTransactionQuerySpec"/> with the supplied query parameters.</summary>
    /// Initializes a new instance of the InventoryTransactionQuerySpec class.
    /// <param name="query">The query.</param>
    public InventoryTransactionQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <summary>Applies transaction-specific filters (type, product, warehouse, quantity range) to the query.</summary>
    /// Applies query filters to the specification.
    /// <param name="query">The query.</param>
    /// <returns>The filtered <see cref="IQueryable{InventoryTransaction}"/>.</returns>
    public override IQueryable<InventoryTransaction> ApplyFilters(IQueryable<InventoryTransaction> query)
    {
        // Apply transaction-specific filters
        if (Query.Filters?.TryGetValue(nameof(InventoryTransaction.TransactionType), out var typeFilter) == true && !string.IsNullOrEmpty(typeFilter))
            query = query.Where(t => t.TransactionType.ToString().Contains(typeFilter, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(InventoryTransaction.ProductId), out var productIdFilter) == true)
        {
            if (Guid.TryParse(productIdFilter.ToString(), out var productId))
                query = query.Where(t => t.ProductId == productId);
        }

        if (Query.Filters?.TryGetValue(nameof(InventoryTransaction.WarehouseId), out var warehouseIdFilter) == true)
        {
            if (Guid.TryParse(warehouseIdFilter.ToString(), out var warehouseId))
                query = query.Where(t => t.WarehouseId == warehouseId);
        }

        if (Query.Filters?.TryGetValue(MinQuantityFilterKey, out var minQty) == true)
        {
            if (int.TryParse(minQty.ToString(), out var qty))
                query = query.Where(t => t.QuantityChange >= qty);
        }

        if (Query.Filters?.TryGetValue(MaxQuantityFilterKey, out var maxQty) == true)
        {
            if (int.TryParse(maxQty.ToString(), out var qty))
                query = query.Where(t => t.QuantityChange <= qty);
        }

        return query;
    }
}
