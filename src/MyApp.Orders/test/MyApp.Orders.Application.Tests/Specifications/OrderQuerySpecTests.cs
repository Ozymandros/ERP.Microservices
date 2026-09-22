using FluentAssertions;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Orders.Application.Tests.Specifications;

/// <summary>Unit tests for <see cref="MyApp.Orders.Domain.Specifications.OrderQuerySpec"/>.</summary>
public class OrderQuerySpecTests
{
    /// <summary>Creates a test data set of orders for use in filter tests.</summary>
    /// <returns>An in-memory queryable collection of test orders.</returns>
    private static IQueryable<Order> CreateTestData()
    {
        var sourceId1 = Guid.NewGuid();
        var sourceId2 = Guid.NewGuid();
        var targetId1 = Guid.NewGuid();
        var externalOrderId = Guid.NewGuid();

        return new List<Order>
        {
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-001", Status = OrderStatus.Draft, Type = OrderType.Inbound, SourceId = sourceId1, TargetId = targetId1, ExternalOrderId = externalOrderId },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-002", Status = OrderStatus.Approved, Type = OrderType.Outbound, SourceId = sourceId1, TargetId = targetId1 },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-003", Status = OrderStatus.Draft, Type = OrderType.Inbound, SourceId = sourceId2, TargetId = targetId1 },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-004", Status = OrderStatus.Completed, Type = OrderType.Outbound, SourceId = sourceId2, TargetId = targetId1 }
        }.AsQueryable();
    }

    /// <summary>Verifies that filtering by OrderNumber returns only the matching orders.</summary>
    [Fact]
    public void ApplyFilters_WithOrderNumberFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "ORD-001" } };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().OrderNumber.Should().Be("ORD-001");
    }

    /// <summary>Verifies that filtering by Status returns only orders with that status.</summary>
    [Fact]
    public void ApplyFilters_WithStatusFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", OrderStatus.Draft.ToString() } };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.Status == OrderStatus.Draft).Should().BeTrue();
    }

    /// <summary>Verifies that filtering by Type returns only orders of that type.</summary>
    [Fact]
    public void ApplyFilters_WithTypeFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Type", OrderType.Inbound.ToString() } };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.Type == OrderType.Inbound).Should().BeTrue();
    }

    /// <summary>Verifies that filtering by SourceId returns only orders with that source.</summary>
    [Fact]
    public void ApplyFilters_WithSourceIdFilter_ReturnsFilteredOrders()
    {
        var data = CreateTestData();
        var sourceId = data.First().SourceId;
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SourceId", sourceId!.Value.ToString() } };
        var spec = new OrderQuerySpec(querySpec);

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.SourceId == sourceId).Should().BeTrue();
    }

    /// <summary>Verifies that a search term filters orders by order number.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingOrders()
    {
        var querySpec = new QuerySpec { SearchTerm = "ORD-001" };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

    /// <summary>Verifies that sorting by OrderNumber returns orders in ascending order.</summary>
    [Fact]
    public void Apply_WithSortByOrderNumber_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "OrderNumber", SortDesc = false };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(o => o.OrderNumber);
    }
}
