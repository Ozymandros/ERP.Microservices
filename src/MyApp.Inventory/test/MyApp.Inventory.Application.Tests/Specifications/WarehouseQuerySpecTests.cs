using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Specifications;

/// <summary>Unit tests for <see cref="WarehouseQuerySpec"/> filtering, searching, sorting, and pagination behaviour.</summary>
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

    /// <summary>Verifies that ApplyFilters returns only warehouses whose name contains the specified filter value.</summary>
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

    /// <summary>Verifies that ApplyFilters returns only warehouses whose location contains the specified filter value.</summary>
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

    /// <summary>Verifies that ApplyFilters returns warehouses whose name or location matches the search term.</summary>
    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingWarehouses()
    {
        var querySpec = new QuerySpec { SearchTerm = "Warehouse" };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
    }

    /// <summary>Verifies that ApplyFilters returns all warehouses when the search term is empty.</summary>
    [Fact]
    public void ApplyFilters_WithEmptySearchTerm_ReturnsAllWarehouses()
    {
        var querySpec = new QuerySpec { SearchTerm = "" };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(4);
    }

    /// <summary>Verifies that Apply sorts warehouses by name in ascending order.</summary>
    [Fact]
    public void Apply_WithSortByName_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(w => w.Name);
    }

    /// <summary>Verifies that Apply returns a page of results no larger than the specified page size.</summary>
    [Fact]
    public void Apply_WithPagination_ReturnsPaginatedResults()
    {
        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new WarehouseQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().HaveCountLessThanOrEqualTo(2);
    }
}
