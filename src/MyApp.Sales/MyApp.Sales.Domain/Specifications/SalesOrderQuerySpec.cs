using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Domain.Specifications;

/// <summary>
/// Specification for querying sales orders with support for filtering, sorting, and pagination.
/// </summary>
public class SalesOrderQuerySpec : BaseSpecification<SalesOrder>
{
    private const string MinTotalFilterKey = $"{nameof(SalesOrder.TotalAmount)}Min";
    private const string MaxTotalFilterKey = $"{nameof(SalesOrder.TotalAmount)}Max";

    public SalesOrderQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<SalesOrder> Apply(IQueryable<SalesOrder> query)
    {
        // Apply sales order-specific filters
        if (Query.Filters?.TryGetValue(nameof(SalesOrder.OrderNumber), out var orderNumberFilter) == true)
            query = query.Where(so => so.OrderNumber.ToLower().Contains(orderNumberFilter.ToString()!.ToLower()));

        if (Query.Filters?.TryGetValue(nameof(SalesOrder.CustomerId), out var customerIdFilter) == true)
        {
            if (Guid.TryParse(customerIdFilter.ToString(), out var customerId))
                query = query.Where(so => so.CustomerId == customerId);
        }

        if (Query.Filters?.TryGetValue(nameof(SalesOrder.Status), out var statusFilter) == true)
        {
            if (Enum.TryParse<SalesOrderStatus>(statusFilter.ToString(), out var status))
                query = query.Where(so => so.Status == status);
        }

        if (Query.Filters?.TryGetValue(MinTotalFilterKey, out var minTotal) == true)
        {
            if (decimal.TryParse(minTotal.ToString(), out var total))
                query = query.Where(so => so.TotalAmount >= total);
        }

        if (Query.Filters?.TryGetValue(MaxTotalFilterKey, out var maxTotal) == true)
        {
            if (decimal.TryParse(maxTotal.ToString(), out var total))
                query = query.Where(so => so.TotalAmount <= total);
        }

        // Apply search (searches in order number)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(so => so.OrderNumber.ToLower().Contains(term));
        }

        return ApplyPaginationAndSorting(query);
    }
}
