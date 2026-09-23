using FluentAssertions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Auth.Application.Tests.Specifications;

/// <summary>
/// Unit tests for <see cref="RoleQuerySpec"/> filter and sort behaviour.
/// </summary>
public class RoleQuerySpecTests
{
    private static IQueryable<ApplicationRole> CreateTestData()
    {
        return new List<ApplicationRole>
        {
            new ApplicationRole("Admin") { Description = "Administrator role", Id = Guid.NewGuid() },
            new ApplicationRole("User") { Description = "Standard user role", Id = Guid.NewGuid() },
            new ApplicationRole("Manager") { Description = "Manager role", Id = Guid.NewGuid() },
            new ApplicationRole("Guest") { Description = "Guest user role", Id = Guid.NewGuid() }
        }.AsQueryable();
    }

    /// <summary>
    /// Verifies that filtering by name returns only roles whose name contains the filter value.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithNameFilter_ReturnsFilteredRoles()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Admin" } };
        var spec = new RoleQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Admin");
    }

    /// <summary>
    /// Verifies that filtering by description returns roles whose description contains the filter value.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithDescriptionFilter_ReturnsFilteredRoles()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Description", "user" } };
        var spec = new RoleQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2); // User and Guest roles
    }

    /// <summary>
    /// Verifies that a search term matching role names or descriptions returns the correct roles.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithSearchTerm_ReturnsMatchingRoles()
    {
        var querySpec = new QuerySpec { SearchTerm = "role" };
        var spec = new RoleQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(4); // All roles contain "role" in description
    }

    /// <summary>
    /// Verifies that sorting by name in ascending order produces the correct sort order.
    /// </summary>
    [Fact]
    public void Apply_WithSortByName_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new RoleQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(r => r.Name);
    }
}
