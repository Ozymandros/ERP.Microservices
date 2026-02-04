using FluentAssertions;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Sales.Application.Tests.Specifications;

public class SalesOrderQuerySpecTests
{
    private IQueryable<SalesOrder> CreateTestData()
    {
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();

        return new List<SalesOrder>
        {
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-001", CustomerId = customerId1, Status = SalesOrderStatus.Draft, TotalAmount = 100.00m },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-002", CustomerId = customerId1, Status = SalesOrderStatus.Confirmed, TotalAmount = 200.00m },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-003", CustomerId = customerId2, Status = SalesOrderStatus.Draft, TotalAmount = 150.00m },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "QUOTE-001", CustomerId = customerId2, Status = SalesOrderStatus.Draft, TotalAmount = 300.00m, IsQuote = true }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithOrderNumberFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "SO-001" } };
        var spec = new SalesOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().OrderNumber.Should().Be("SO-001");
    }

    [Fact]
    public void ApplyFilters_WithCustomerIdFilter_ReturnsFilteredOrders()
    {
        var data = CreateTestData();
        var customerId = data.First().CustomerId;
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "CustomerId", customerId.ToString() } };
        var spec = new SalesOrderQuerySpec(querySpec);

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.CustomerId == customerId).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithStatusFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", SalesOrderStatus.Draft.ToString() } };
        var spec = new SalesOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(o => o.Status == SalesOrderStatus.Draft).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithMinTotalFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "TotalAmountMin", "150" } };
        var spec = new SalesOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(o => o.TotalAmount >= 150m).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingOrders()
    {
        var querySpec = new QuerySpec { SearchTerm = "QUOTE" };
        var spec = new SalesOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().OrderNumber.Should().Contain("QUOTE");
    }

    [Fact]
    public void Apply_WithSortByTotalAmount_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "TotalAmount", SortDesc = true };
        var spec = new SalesOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInDescendingOrder(o => o.TotalAmount);
    }
}
