using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Purchasing.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Purchasing.Tests.Repositories;

/// <summary>Integration tests for <see cref="SupplierRepository"/> using an in-memory database.</summary>
public class SupplierRepositoryTests
{
    private readonly PurchasingDbContext _context;
    private readonly SupplierRepository _repository;

    /// <summary>Initializes a new instance of the <see cref="SupplierRepositoryTests"/> class, creating a fresh in-memory context and seeding test data.</summary>
    public SupplierRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new SupplierRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    /// <summary>Creates and persists a <see cref="Supplier"/> with the given name and email for use in a test.</summary>
    /// <param name="name">The supplier name.</param>
    /// <param name="email">The supplier email address.</param>
    /// <returns>The persisted <see cref="Supplier"/> entity.</returns>
    private Supplier CreateTestSupplier(string name = "Test Supplier", string email = "test@supplier.com")
    {
        var supplier = new Supplier(Guid.NewGuid())
        {
            Name = name,
            ContactName = "Contact Person",
            Email = email,
            PhoneNumber = "555-1234",
            Address = "123 Supplier St"
        };
        _context.Suppliers.Add(supplier);
        _context.SaveChanges();
        return supplier;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.GetByIdAsync"/> returns the supplier when a valid ID is provided.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSupplier()
    {
        // Arrange
        var supplier = CreateTestSupplier("ABC Suppliers", "abc@supplier.com");

        // Act
        var result = await _repository.GetByIdAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result.Id);
        Assert.Equal("ABC Suppliers", result.Name);
        Assert.Equal("abc@supplier.com", result.Email);
    }

    /// <summary>Verifies that <see cref="SupplierRepository.GetByIdAsync"/> returns null when the ID does not exist.</summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByEmailAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.GetByEmailAsync"/> returns the supplier when the email exists.</summary>
    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsSupplier()
    {
        // Arrange
        CreateTestSupplier("XYZ Suppliers", "xyz@supplier.com");

        // Act
        var result = await _repository.GetByEmailAsync("xyz@supplier.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("xyz@supplier.com", result.Email);
        Assert.Equal("XYZ Suppliers", result.Name);
    }

    /// <summary>Verifies that <see cref="SupplierRepository.GetByEmailAsync"/> returns null when the email does not exist.</summary>
    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@supplier.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByNameAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.GetByNameAsync"/> returns all suppliers whose names contain the search term.</summary>
    [Fact]
    public async Task GetByNameAsync_WithPartialName_ReturnsMatchingSuppliers()
    {
        // Arrange
        CreateTestSupplier("Global Suppliers Inc", "global@supplier.com");
        CreateTestSupplier("Global Trade Co", "globaltrade@supplier.com");
        CreateTestSupplier("Local Distributors", "local@supplier.com");

        // Act
        var result = await _repository.GetByNameAsync("Global");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, s => Assert.Contains("Global", s.Name));
    }

    /// <summary>Verifies that <see cref="SupplierRepository.GetByNameAsync"/> returns an empty collection when no supplier names match.</summary>
    [Fact]
    public async Task GetByNameAsync_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        CreateTestSupplier("ABC Suppliers", "abc@supplier.com");

        // Act
        var result = await _repository.GetByNameAsync("NonExistent");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.AddAsync"/> persists a new supplier to the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidSupplier_CreatesSupplier()
    {
        // Arrange
        var supplier = new Supplier(Guid.NewGuid())
        {
            Name = "New Supplier",
            ContactName = "John Doe",
            Email = "new@supplier.com",
            PhoneNumber = "555-9999",
            Address = "456 New St"
        };

        // Act
        await _repository.AddAsync(supplier);
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Supplier", result.Name);
        Assert.Equal("new@supplier.com", result.Email);
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.UpdateAsync"/> persists changes to an existing supplier.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingSupplier_UpdatesSupplierData()
    {
        // Arrange
        var supplier = CreateTestSupplier("Original Name", "original@supplier.com");
        supplier.Name = "Updated Name";
        supplier.Email = "updated@supplier.com";

        // Act
        await _repository.UpdateAsync(supplier);
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("updated@supplier.com", result.Email);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.DeleteAsync"/> removes the supplier from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesSupplier()
    {
        // Arrange
        var supplier = CreateTestSupplier("Delete Me", "delete@supplier.com");

        // Act
        await _repository.DeleteAsync(supplier);
        await _context.SaveChangesAsync();
        var result = await _context.Suppliers.FindAsync(supplier.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.GetAllAsync"/> returns all suppliers in the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllSuppliers()
    {
        // Arrange
        CreateTestSupplier("Supplier 1", "supplier1@example.com");
        CreateTestSupplier("Supplier 2", "supplier2@example.com");
        CreateTestSupplier("Supplier 3", "supplier3@example.com");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.GetAllPaginatedAsync"/> returns the correct page of suppliers with accurate pagination metadata.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        CreateTestSupplier("PAGE-001", "page1@supplier.com");
        CreateTestSupplier("PAGE-002", "page2@supplier.com");
        CreateTestSupplier("PAGE-003", "page3@supplier.com");
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region QueryAsync Tests

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> filters suppliers by the free-text search term.</summary>
    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        CreateTestSupplier("Widget Supplier", "widget@supplier.com");
        CreateTestSupplier("Gadget Supplier", "gadget@supplier.com");
        CreateTestSupplier("Other Supplier", "other@supplier.com");
        var querySpec = new QuerySpec { SearchTerm = "Widget" };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(s => s.Name.Contains("Widget", StringComparison.OrdinalIgnoreCase) ||
                                          s.Email.Contains("Widget", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> filters suppliers by the name filter.</summary>
    [Fact]
    public async Task QueryAsync_WithNameFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestSupplier("Filter Supplier", "filter@supplier.com");
        CreateTestSupplier("Other Supplier", "other@supplier.com");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Filter" } };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(s => s.Name.Contains("Filter", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> filters suppliers by the email filter.</summary>
    [Fact]
    public async Task QueryAsync_WithEmailFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestSupplier("Supplier 1", "filter@supplier.com");
        CreateTestSupplier("Supplier 2", "other@supplier.com");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Email", "filter" } };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(s => s.Email.Contains("filter", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> filters suppliers by the contact name filter.</summary>
    [Fact]
    public async Task QueryAsync_WithContactNameFilter_ShouldFilterResults()
    {
        // Arrange
        var supplier1 = CreateTestSupplier("Supplier 1", "supplier1@example.com");
        supplier1.ContactName = "Filter Contact";
        _context.SaveChanges();
        CreateTestSupplier("Supplier 2", "supplier2@example.com");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "ContactName", "Filter" } };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(s => s.ContactName.Contains("Filter", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> returns the correct page of results when pagination is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        CreateTestSupplier("PAGE-QUERY-001", "page1@supplier.com");
        CreateTestSupplier("PAGE-QUERY-002", "page2@supplier.com");
        CreateTestSupplier("PAGE-QUERY-003", "page3@supplier.com");
        CreateTestSupplier("PAGE-QUERY-004", "page4@supplier.com");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(4);
    }

    /// <summary>Verifies that <see cref="SupplierRepository.QueryAsync"/> returns suppliers sorted in ascending order when a sort field is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        CreateTestSupplier("Zebra Supplier", "zebra@supplier.com");
        CreateTestSupplier("Alpha Supplier", "alpha@supplier.com");
        CreateTestSupplier("Beta Supplier", "beta@supplier.com");
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new SupplierQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var names = result.Items.Select(s => s.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        names.Should().BeEquivalentTo(sortedNames);
    }

    #endregion
}

