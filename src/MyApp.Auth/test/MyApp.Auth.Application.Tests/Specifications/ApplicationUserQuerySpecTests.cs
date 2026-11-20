using FluentAssertions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using System.Linq;
using Xunit;

namespace MyApp.Auth.Application.Tests.Specifications;

/// <summary>
/// Unit tests for <see cref="ApplicationUserQuerySpec"/> filter and sort behaviour.
/// </summary>
public class ApplicationUserQuerySpecTests
{
    private static IQueryable<ApplicationUser> CreateTestData()
    {
        return new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@example.com", FirstName = "John", LastName = "Doe", IsActive = true, IsExternalLogin = false },
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@example.com", FirstName = "Jane", LastName = "Smith", IsActive = true, IsExternalLogin = true },
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user3", Email = "user3@example.com", FirstName = "Bob", LastName = "Johnson", IsActive = false, IsExternalLogin = false },
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin", Email = "admin@example.com", FirstName = "Admin", LastName = "User", IsActive = true, IsExternalLogin = false }
        }.AsQueryable();
    }

    /// <summary>
    /// Verifies that filtering by IsActive returns only active users.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithIsActiveFilter_ReturnsFilteredUsers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "IsActive", "true" } };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(3);
        result.All(u => u.IsActive).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that filtering by email returns only users whose email contains the filter value.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithEmailFilter_ReturnsFilteredUsers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Email", "user1" } };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().Email.Should().Contain("user1");
    }

    /// <summary>
    /// Verifies that filtering by username returns only users whose username contains the filter value.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithUserNameFilter_ReturnsFilteredUsers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "UserName", "admin" } };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().UserName.Should().Contain("admin");
    }

    /// <summary>
    /// Verifies that filtering by IsExternalLogin returns only external-login users.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithIsExternalLoginFilter_ReturnsFilteredUsers()
    {
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "IsExternalLogin", "true" } };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
        result.First().IsExternalLogin.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a search term matching a user's first or last name returns those users.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithSearchTermInFirstName_ReturnsMatchingUsers()
    {
        var querySpec = new QuerySpec { SearchTerm = "John" };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(2); // John Doe and Bob Johnson
    }

    /// <summary>
    /// Verifies that a search term matching a user's email returns that user.
    /// </summary>
    [Fact]
    public void ApplyFilters_WithSearchTermInEmail_ReturnsMatchingUsers()
    {
        var querySpec = new QuerySpec { SearchTerm = "admin" };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.ApplyFilters(data).ToList();

        result.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that sorting by email in ascending order produces the correct sort order.
    /// </summary>
    [Fact]
    public void Apply_WithSortByEmail_SortsCorrectly()
    {
        var querySpec = new QuerySpec { SortBy = "Email", SortDesc = false };
        var spec = new ApplicationUserQuerySpec(querySpec);
        var data = CreateTestData();

        var result = spec.Apply(data).ToList();

        result.Should().BeInAscendingOrder(u => u.Email);
    }
}
