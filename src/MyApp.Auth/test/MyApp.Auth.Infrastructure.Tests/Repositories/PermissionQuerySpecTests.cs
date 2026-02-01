using System.Linq;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Specifications;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Auth.Tests.Repositories;

public class PermissionQuerySpecTests
{
    private readonly AuthDbContext _context;
    private readonly PermissionRepository _repository;

    public PermissionQuerySpecTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new PermissionRepository(_context);
    }

    private void SeedData()
    {
        var permissions = new List<Permission>
        {
            new(Guid.NewGuid()) { Module = "Users", Action = "Read", Description = "Can read users" },
            new(Guid.NewGuid()) { Module = "Users", Action = "Write", Description = "Can write users" },
            new(Guid.NewGuid()) { Module = "Roles", Action = "Read", Description = "Can read roles" },
            new(Guid.NewGuid()) { Module = "Roles", Action = "Write", Description = "Can write roles" },
            new(Guid.NewGuid()) { Module = "Inventory", Action = "Read", Description = "Can read inventory" }
        };
        _context.Permissions.AddRange(permissions);
        _context.SaveChanges();
    }

    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        SeedData();
        var querySpec = new QuerySpec { SearchTerm = "users" };
        var spec = new PermissionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Contains("Users", p.Module));
    }

    [Fact]
    public async Task QueryAsync_WithFieldFilter_ShouldFilterResults()
    {
        // Arrange
        SeedData();
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Module", "Roles" } };
        var spec = new PermissionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("Roles", p.Module));
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        SeedData(); // 5 permissions
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new PermissionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        SeedData();
        var querySpec = new QuerySpec { SortBy = "Module", SortDesc = true };
        var spec = new PermissionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        var modules = result.Items.Select(p => p.Module).ToList();
        var sortedModules = modules.OrderByDescending(m => m).ToList();
        Assert.Equal(sortedModules, modules);
    }
}
