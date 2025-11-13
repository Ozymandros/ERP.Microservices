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

    public InventoryTransactionQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<InventoryTransaction> Apply(IQueryable<InventoryTransaction> query)
    {
        // Apply transaction-specific filters
        if (Query.Filters?.TryGetValue(nameof(InventoryTransaction.TransactionType), out var typeFilter) == true)
            query = query.Where(t => t.TransactionType.ToString().ToLower().Contains(typeFilter.ToString()!.ToLower()));

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

        return ApplyPaginationAndSorting(query);
    }
}
