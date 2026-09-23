using FluentAssertions;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Specifications;

/// <summary>Unit tests for the <see cref="MyApp.Purchasing.Domain.Specifications.PurchaseOrderQuerySpec"/> specification.</summary>
public class PurchaseOrderQuerySpecTests
{
    /// <summary>Creates a fixed set of <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrder"/> instances for use in tests.</summary>
    /// <returns>An <see cref="IQueryable{T}"/> containing the test purchase orders.</returns>
    private static IQueryable<PurchaseOrder> CreateTestData()
    {
        var supplierId1 = Guid.NewGuid();
        var supplierId2 = Guid.NewGuid();

        return new List<PurchaseOrder>
        {
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-001", SupplierId = supplierId1, Status = PurchaseOrderStatus.Draft, TotalAmount = 500.00m },
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-002", SupplierId = supplierId1, Status = PurchaseOrderStatus.Approved, TotalAmount = 750.00m },
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-003", SupplierId = supplierId2, Status = PurchaseOrderStatus.Draft, TotalAmount = 600.00m },
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-004", SupplierId = supplierId2, Status = PurchaseOrderStatus.Received, TotalAmount = 400.00m }
        }.AsQueryable();
    }

    /// <summary>Verifies that filtering by order number returns only orders matching that number.</summary>
    [Fact]
    public void ApplyFilters_WithOrderNumberFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "OrderNumber", "PO-001" } };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().OrderNumber.Should().Be("PO-001");
    }

    /// <summary>Verifies that filtering by supplier ID returns only orders for that supplier.</summary>
    [Fact]
    public void ApplyFilters_WithSupplierIdFilter_ReturnsFilteredOrders()
    {
        var data = CreateTestData();
        var supplierId = data.First().SupplierId;
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SupplierId", supplierId.ToString() } };
        var spec = new PurchaseOrderQuerySpec(querySpec);

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.SupplierId == supplierId).Should().BeTrue();
    }

    /// <summary>Verifies that filtering by status returns only orders in that status.</summary>
    [Fact]
    public void ApplyFilters_WithStatusFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Status", PurchaseOrderStatus.Draft.ToString() } };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.Status == PurchaseOrderStatus.Draft).Should().BeTrue();
    }

    /// <summary>Verifies that filtering by minimum total amount returns only orders at or above that amount.</summary>
    [Fact]
    public void ApplyFilters_WithMinTotalFilter_ReturnsFilteredOrders()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "TotalAmountMin", "600" } };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(o => o.TotalAmount >= 600m).Should().BeTrue();
    }

    /// <summary>Verifies that a free-text search term filters orders by order number.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingOrders()
    {
        var querySpec = new QuerySpec { SearchTerm = "PO-001" };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

    /// <summary>Verifies that sorting by TotalAmount returns orders in ascending order.</summary>
    [Fact]
    public void Apply_WithSortByTotalAmount_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "TotalAmount", SortDesc = false };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(o => o.TotalAmount);
    }
}
