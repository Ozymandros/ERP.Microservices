using FluentAssertions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Auth.Application.Tests.Specifications;

/// <summary>
/// Unit tests for <see cref="PermissionQuerySpec"/> filter and sort behaviour.
/// </summary>
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

    /// <summary>
    /// Verifies that filtering by module returns only permissions belonging to that module.
    /// </summary>
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

    /// <summary>
    /// Verifies that filtering by action returns only permissions with that action.
    /// </summary>
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

    /// <summary>
    /// Verifies that filtering by description returns permissions whose description contains the filter value.
    /// </summary>
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

    /// <summary>
    /// Verifies that a search term matches permissions by module, action, or description.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingPermissions()
    {
        var querySpec = new QuerySpec { SearchTerm = "Create" };
        var spec = new PermissionQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that combining multiple filters returns only permissions that satisfy all conditions.
    /// </summary>
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

    /// <summary>
    /// Verifies that sorting by module in ascending order produces the correct sort order.
    /// </summary>
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
