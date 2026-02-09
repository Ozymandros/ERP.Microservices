using FluentAssertions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Auth.Application.Tests.Specifications;

public class PermissionQuerySpecTests
{
    private static IQueryable<Permission> CreateTestData()
    {
        return new List<Permission>
        {
            new Permission(Guid.NewGuid()) { Module = "Orders", Action = "Create", Description = "Create orders" },
            new Permission(Guid.NewGuid()) { Module = "Orders", Action = "Read", Description = "Read orders" },
            new Permission(Guid.NewGuid()) { Module = "Inventory", Action = "Create", Description = "Create inventory items" },
            new Permission(Guid.NewGuid()) { Module = "Inventory", Action = "Update", Description = "Update inventory items" },
            new Permission(Guid.NewGuid()) { Module = "Sales", Action = "Create", Description = "Create sales orders" }
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithModuleFilter_ReturnsFilteredPermissions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Module", "Orders" } };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2);
        result.All(p => p.Module == "Orders").Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithActionFilter_ReturnsFilteredPermissions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Action", "Create" } };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(p => p.Action == "Create").Should().BeTrue();
    }

    [Fact]
    public void ApplyFilters_WithDescriptionFilter_ReturnsFilteredPermissions()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Description", "orders" } };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3); // Orders Create, Orders Read, Sales Create
    }

    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingPermissions()
    {
        var querySpec = new QuerySpec { SearchTerm = "Create" };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void ApplyFilters_WithMultipleFilters_ReturnsIntersection()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Module", "Orders" }, { "Action", "Create" } };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Module.Should().Be("Orders");
        result.First().Action.Should().Be("Create");
    }

    [Fact]
    public void Apply_WithSortByModule_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Module", SortDesc = false };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(p => p.Module);
    }
}
