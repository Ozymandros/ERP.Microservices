using FluentAssertions;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Specifications;

public class PurchaseOrderQuerySpecTests
{
    private IQueryable<PurchaseOrder> CreateTestData()
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

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingOrders()
    {
        var querySpec = new QuerySpec { SearchTerm = "PO-001" };
        var spec = new PurchaseOrderQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

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
