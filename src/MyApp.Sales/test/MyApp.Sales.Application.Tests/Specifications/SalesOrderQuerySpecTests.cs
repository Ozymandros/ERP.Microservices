using FluentAssertions;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Sales.Application.Tests.Specifications;

/// <summary>Unit tests for <see cref="SalesOrderQuerySpec"/> filtering, sorting, and search behaviour.</summary>
public class SalesOrderQuerySpecTests
{
    private static IQueryable<SalesOrder> CreateTestData()
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

    /// <summary>Verifies that ApplyFilters returns only orders whose order number contains the supplied filter value.</summary>
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

    /// <summary>Verifies that ApplyFilters returns only orders belonging to the specified customer identifier.</summary>
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

    /// <summary>Verifies that ApplyFilters returns only orders matching the specified status value.</summary>
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

    /// <summary>Verifies that ApplyFilters returns only orders whose TotalAmount is greater than or equal to the TotalAmountMin filter.</summary>
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

    /// <summary>Verifies that ApplyFilters matches orders by order number when a free-text search term is supplied.</summary>
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

    /// <summary>Verifies that Apply sorts orders in descending order by TotalAmount when SortBy is "TotalAmount" and SortDesc is true.</summary>
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
