using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using System;
using System.Linq;

namespace MyApp.Orders.Domain.Specifications;

/// <summary>
/// Specification for querying orders with support for filtering, sorting, and pagination.
/// </summary>
public class OrderQuerySpec : BaseSpecification<Order>
{
    public OrderQuerySpec(QuerySpec query) : base(query) { }

    public override IQueryable<Order> Apply(IQueryable<Order> query)
    {
        // Filter by OrderNumber
        if (Query.Filters?.TryGetValue(nameof(Order.OrderNumber), out var orderNumberFilter) == true)
            query = query.Where(o => o.OrderNumber.ToLower().Contains(orderNumberFilter.ToLower()));

        // Filter by Status
        if (Query.Filters?.TryGetValue(nameof(Order.Status), out var statusFilter) == true &&
            Enum.TryParse<OrderStatus>(statusFilter, out var status))
            query = query.Where(o => o.Status == status);

        // Filter by Type
        if (Query.Filters?.TryGetValue(nameof(Order.Type), out var typeFilter) == true &&
            Enum.TryParse<OrderType>(typeFilter, out var type))
            query = query.Where(o => o.Type == type);

        // Filter by SourceId
        if (Query.Filters?.TryGetValue(nameof(Order.SourceId), out var sourceIdFilter) == true &&
            Guid.TryParse(sourceIdFilter, out var sourceId))
            query = query.Where(o => o.SourceId == sourceId);

        // Filter by TargetId
        if (Query.Filters?.TryGetValue(nameof(Order.TargetId), out var targetIdFilter) == true &&
            Guid.TryParse(targetIdFilter, out var targetId))
            query = query.Where(o => o.TargetId == targetId);

        // Filter by ExternalOrderId
        if (Query.Filters?.TryGetValue(nameof(Order.ExternalOrderId), out var externalOrderIdFilter) == true &&
            Guid.TryParse(externalOrderIdFilter, out var externalOrderId))
            query = query.Where(o => o.ExternalOrderId == externalOrderId);

        // Search (OrderNumber)
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var term = Query.SearchTerm.ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term));
        }

        return ApplyPaginationAndSorting(query);
    }
}
