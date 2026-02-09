using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Specifications;

public class InventoryTransactionQuerySpecTests
{
    private static IQueryable<InventoryTransaction> CreateTestData()
    {
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var warehouseId1 = Guid.NewGuid();
        var warehouseId2 = Guid.NewGuid();

        return new List<InventoryTransaction>
        {
            new InventoryTransaction(Guid.NewGuid()) { ProductId = productId1, WarehouseId = warehouseId1, TransactionType = TransactionType.Inbound, QuantityChange = 10 },
            new InventoryTransaction(Guid.NewGuid()) { ProductId = productId1, WarehouseId = warehouseId2, TransactionType = TransactionType.Outbound, QuantityChange = -5 },
            new InventoryTransaction(Guid.NewGuid()) { ProductId = productId2, WarehouseId = warehouseId1, TransactionType = TransactionType.Adjustment, QuantityChange = 20 },
            new InventoryTransaction(Guid.NewGuid()) { ProductId = productId2, WarehouseId = warehouseId2, TransactionType = TransactionType.Inbound, QuantityChange = 15 }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithTransactionTypeFilter_ReturnsFilteredTransactions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "TransactionType", "Inbound" } };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(t => t.TransactionType == TransactionType.Inbound).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithProductIdFilter_ReturnsFilteredTransactions()
    {
        var data = CreateTestData();
        var productId = data.First().ProductId;
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "ProductId", productId.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(t => t.ProductId == productId).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithWarehouseIdFilter_ReturnsFilteredTransactions()
    {
        var data = CreateTestData();
        var warehouseId = data.First().WarehouseId;
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "WarehouseId", warehouseId.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(t => t.WarehouseId == warehouseId).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithMinQuantityFilter_ReturnsFilteredTransactions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "QuantityChangeMin", "10" } };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(t => t.QuantityChange >= 10).Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithMaxQuantityFilter_ReturnsFilteredTransactions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "QuantityChangeMax", "15" } };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(t => t.QuantityChange <= 15).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithSortByQuantity_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "QuantityChange", SortDesc = true };
        var spec = new InventoryTransactionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInDescendingOrder(t => t.QuantityChange);
    }
}
