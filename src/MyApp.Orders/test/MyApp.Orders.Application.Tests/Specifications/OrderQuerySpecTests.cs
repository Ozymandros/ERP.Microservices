using FluentAssertions;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Orders.Application.Tests.Specifications;

public class OrderQuerySpecTests
{
    private IQueryable<Order> CreateTestData()
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

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingOrders()
    {
        var querySpec = new QuerySpec { SearchTerm = "ORD-001" };
        var spec = new OrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

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
