using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Specifications;

public class WarehouseQuerySpecTests
{
    private static IQueryable<Warehouse> CreateTestData()
    {
        return new List<Warehouse>
        {
            new Warehouse(Guid.NewGuid()) { Name = "Main Warehouse", Location = "New York" },
            new Warehouse(Guid.NewGuid()) { Name = "Secondary Warehouse", Location = "Los Angeles" },
            new Warehouse(Guid.NewGuid()) { Name = "Distribution Center", Location = "Chicago" },
            new Warehouse(Guid.NewGuid()) { Name = "Storage Facility", Location = "Houston" }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithNameFilter_ReturnsFilteredWarehouses()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Main" } };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Main");
    }

    [Fact]
    public void ApplyFilters_WithLocationFilter_ReturnsFilteredWarehouses()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Location", "York" } };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Location.Should().Contain("York");
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingWarehouses()
    {
        var querySpec = new QuerySpec { SearchTerm = "Warehouse" };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplyFilters_WithEmptySearchTerm_ReturnsAllWarehouses()
    {
        var querySpec = new QuerySpec { SearchTerm = "" };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void Apply_WithSortByName_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(w => w.Name);
    }

    [Fact]
    public void Apply_WithPagination_ReturnsPaginatedResults()
    {
        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().HaveCountLessOrEqualTo(2);
    }
}
