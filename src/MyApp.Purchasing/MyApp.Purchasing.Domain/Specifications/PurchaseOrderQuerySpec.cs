using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Domain.Specifications;

/// <summary>
/// Specification for querying purchase orders with support for filtering, sorting, and pagination.
/// </summary>
public class PurchaseOrderQuerySpec : BaseSpecification<PurchaseOrder>
{
    private const string MinTotalFilterKey = $"{nameof(PurchaseOrder.TotalAmount)}Min";
    private const string MaxTotalFilterKey = $"{nameof(PurchaseOrder.TotalAmount)}Max";

    public PurchaseOrderQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<PurchaseOrder> Apply(IQueryable<PurchaseOrder> query)
    {
        // Apply purchase order-specific filters
        if (Query.Filters?.TryGetValue(nameof(PurchaseOrder.OrderNumber), out var orderNumberFilter) == true)
            query = query.Where(po => po.OrderNumber.ToLower().Contains(orderNumberFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(PurchaseOrder.SupplierId), out var supplierIdFilter) == true)
        {
            if (Guid.TryParse(supplierIdFilter.ToString(), out var supplierId))
                query = query.Where(po => po.SupplierId == supplierId);
        }

        if (Query.Filters?.TryGetValue(nameof(PurchaseOrder.Status), out var statusFilter) == true)
        {
            if (Enum.TryParse<PurchaseOrderStatus>(statusFilter.ToString(), out var status))
                query = query.Where(po => po.Status == status);
        }

        if (Query.Filters?.TryGetValue(MinTotalFilterKey, out var minTotal) == true)
        {
            if (decimal.TryParse(minTotal.ToString(), out var total))
                query = query.Where(po => po.TotalAmount >= total);
        }

        if (Query.Filters?.TryGetValue(MaxTotalFilterKey, out var maxTotal) == true)
        {
            if (decimal.TryParse(maxTotal.ToString(), out var total))
                query = query.Where(po => po.TotalAmount <= total);
        }

        // Apply search (searches in order number)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(po => po.OrderNumber.ToLower().Contains(term));
        }

        return ApplyPaginationAndSorting(query);
    }
}
