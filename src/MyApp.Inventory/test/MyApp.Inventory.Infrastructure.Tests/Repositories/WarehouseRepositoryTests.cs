using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Inventory.Infrastructure.Data.Repositories;
using MyApp.Inventory.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Inventory.Tests.Repositories;

/// <summary>Integration tests for <see cref="WarehouseRepository"/> using an in-memory EF Core database.</summary>
public class WarehouseRepositoryTests
{
    private readonly InventoryDbContext _context;
    private readonly WarehouseRepository _repository;

    /// <summary>Initialises the in-memory database context and repository, and seeds base warehouse data before each test.</summary>
    public WarehouseRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new WarehouseRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.Warehouses.RemoveRange(_context.Warehouses);
        _context.SaveChanges();

        // Create test warehouses
        var warehouse1 = new Warehouse(Guid.NewGuid())
        {
            Name = "Warehouse 1",
            Location = "Location 1"
        };
        var warehouse2 = new Warehouse(Guid.NewGuid())
        {
            Name = "Warehouse 2",
            Location = "Location 2"
        };
        _context.Warehouses.AddRange(warehouse1, warehouse2);
        _context.SaveChanges();
    }

    private Warehouse CreateTestWarehouse(string name = "Test Warehouse", string location = "Test Location")
    {
        var warehouse = new Warehouse(Guid.NewGuid())
        {
            Name = name,
            Location = location
        };
        _context.Warehouses.Add(warehouse);
        _context.SaveChanges();
        return warehouse;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that GetByIdAsync returns the warehouse when a valid ID is provided.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWarehouse()
    {
        // Arrange
        var warehouse = CreateTestWarehouse("GetById Warehouse", "GetById Location");

        // Act
        var result = await _repository.GetByIdAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(warehouse.Id);
        result.Name.Should().Be("GetById Warehouse");
        result.Location.Should().Be("GetById Location");
    }

    /// <summary>Verifies that GetByIdAsync returns null when no warehouse with the given ID exists.</summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that GetAllAsync returns all warehouses currently in the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllWarehouses()
    {
        // Arrange
        CreateTestWarehouse("List-001", "Location A");
        CreateTestWarehouse("List-002", "Location B");
        CreateTestWarehouse("List-003", "Location C");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(5); // At least 2 seeded + 3 new
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that GetAllPaginatedAsync returns a correctly sized and numbered page of warehouses.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        CreateTestWarehouse("PAGE-001", "Location 1");
        CreateTestWarehouse("PAGE-002", "Location 2");
        CreateTestWarehouse("PAGE-003", "Location 3");
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    /// <summary>Verifies that GetAllPaginatedAsync returns items from the second page when requested.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        CreateTestWarehouse("PAGE2-001", "Location 1");
        CreateTestWarehouse("PAGE2-002", "Location 2");
        CreateTestWarehouse("PAGE2-003", "Location 3");
        var pageNumber = 2;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(pageNumber);
        result.Items.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region GetByNameAsync Tests

    /// <summary>Verifies that GetByNameAsync returns the warehouse when a matching name exists.</summary>
    [Fact]
    public async Task GetByNameAsync_WithExistingName_ReturnsWarehouse()
    {
        // Arrange
        var warehouse = CreateTestWarehouse("Unique Warehouse Name", "Location");

        // Act
        var result = await _repository.GetByNameAsync("Unique Warehouse Name");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Warehouse Name");
        result.Id.Should().Be(warehouse.Id);
    }

    /// <summary>Verifies that GetByNameAsync returns null when no warehouse with the given name exists.</summary>
    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("Non-Existent Warehouse");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new warehouse to the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidWarehouse_CreatesWarehouse()
    {
        // Arrange
        var warehouse = new Warehouse(Guid.NewGuid())
        {
            Name = "New Warehouse",
            Location = "New Location"
        };

        // Act
        var result = await _repository.AddAsync(warehouse);
        var savedWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(warehouse.Id);
        savedWarehouse.Should().NotBeNull();
        savedWarehouse!.Name.Should().Be("New Warehouse");
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that UpdateAsync saves modified warehouse fields to the database.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingWarehouse_UpdatesWarehouseData()
    {
        // Arrange
        var warehouse = CreateTestWarehouse("Original Name", "Original Location");
        warehouse.Name = "Updated Name";
        warehouse.Location = "Updated Location";

        // Act
        var result = await _repository.UpdateAsync(warehouse);
        var updatedWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        updatedWarehouse.Should().NotBeNull();
        updatedWarehouse!.Name.Should().Be("Updated Name");
        updatedWarehouse.Location.Should().Be("Updated Location");
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that DeleteAsync removes the warehouse from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidWarehouse_DeletesWarehouse()
    {
        // Arrange
        var warehouse = CreateTestWarehouse("Delete Me", "Location");

        // Act
        await _repository.DeleteAsync(warehouse);
        await _context.SaveChangesAsync();
        var deletedWarehouse = await _context.Warehouses.FindAsync(warehouse.Id);

        // Assert
        deletedWarehouse.Should().BeNull();
    }

    #endregion

    #region QueryAsync Tests

    /// <summary>Verifies that QueryAsync filters warehouses by a search term applied to name and location fields.</summary>
    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        CreateTestWarehouse("Widget Warehouse", "Widget Location");
        CreateTestWarehouse("Gadget Warehouse", "Gadget Location");
        CreateTestWarehouse("Other Warehouse", "Other Location");
        var querySpec = new QuerySpec { SearchTerm = "Widget" };
        var spec = new WarehouseQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(w => w.Name.Contains("Widget", StringComparison.OrdinalIgnoreCase) ||
                                           w.Location.Contains("Widget", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters warehouses by a name filter and returns only matching results.</summary>
    [Fact]
    public async Task QueryAsync_WithNameFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestWarehouse("Filter Warehouse", "Location");
        CreateTestWarehouse("Other Warehouse", "Location");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Filter" } };
        var spec = new WarehouseQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(w => w.Name.Contains("Filter", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters warehouses by a location filter and returns only matching results.</summary>
    [Fact]
    public async Task QueryAsync_WithLocationFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestWarehouse("Warehouse 1", "Filter Location");
        CreateTestWarehouse("Warehouse 2", "Other Location");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Location", "Filter" } };
        var spec = new WarehouseQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(w => w.Location.Contains("Filter", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync returns the correct page of results when pagination is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        CreateTestWarehouse("PAGE-QUERY-001", "Location");
        CreateTestWarehouse("PAGE-QUERY-002", "Location");
        CreateTestWarehouse("PAGE-QUERY-003", "Location");
        CreateTestWarehouse("PAGE-QUERY-004", "Location");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new WarehouseQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(6);
    }

    /// <summary>Verifies that QueryAsync returns warehouses sorted in ascending order when a sort field is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        CreateTestWarehouse("Zebra Warehouse", "Location");
        CreateTestWarehouse("Alpha Warehouse", "Location");
        CreateTestWarehouse("Beta Warehouse", "Location");
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new WarehouseQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var names = result.Items.Select(w => w.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        names.Should().BeEquivalentTo(sortedNames);
    }

    #endregion
}

