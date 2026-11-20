using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Inventory.Infrastructure.Data.Repositories;
using MyApp.Inventory.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Inventory.Tests.Repositories;

/// <summary>Integration tests for <see cref="ProductRepository"/> using an in-memory EF Core database.</summary>
public class ProductRepositoryTests
{
    private readonly InventoryDbContext _context;
    private readonly ProductRepository _repository;

    /// <summary>Initialises the in-memory database context and repository before each test.</summary>
    public ProductRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new ProductRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

/// <summary>
/// Creates a test product with the given SKU, name, quantity in stock, reorder level, and unit price.
/// </summary>
/// <param name="sku"></param>
/// <param name="name"></param>
/// <param name="quantityInStock"></param>
/// <param name="reorderLevel"></param>
/// <param name="unitPrice"></param>
/// <returns>The created test product.</returns> 
    private Product CreateTestProduct(string sku = "TEST-001", string name = "Test Product", int quantityInStock = 100, int reorderLevel = 10, decimal unitPrice = 25.00m)
    {
        var product = new Product(Guid.NewGuid())
        {
            SKU = sku,
            Name = name,
            Description = "Test Description",
            UnitPrice = unitPrice,
            QuantityInStock = quantityInStock,
            ReorderLevel = reorderLevel
        };
        _context.Products.Add(product);
        _context.SaveChanges();
        return product;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that GetByIdAsync returns the product when a valid ID is provided.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var product = CreateTestProduct("SKU-001", "Product 1");

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("SKU-001", result.SKU);
        Assert.Equal("Product 1", result.Name);
    }

    /// <summary>Verifies that GetByIdAsync returns null when the product ID does not exist in the database.</summary>
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

    #region GetBySkuAsync Tests

    /// <summary>Verifies that GetBySkuAsync returns the product when a matching SKU exists.</summary>
    [Fact]
    public async Task GetBySkuAsync_WithValidSku_ReturnsProduct()
    {
        // Arrange
        CreateTestProduct("SKU-UNIQUE", "Unique Product");

        // Act
        var result = await _repository.GetBySkuAsync("SKU-UNIQUE");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SKU-UNIQUE", result.SKU);
        Assert.Equal("Unique Product", result.Name);
    }

    /// <summary>Verifies that GetBySkuAsync returns null when no product with the given SKU exists.</summary>
    [Fact]
    public async Task GetBySkuAsync_WithNonExistentSku_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySkuAsync("NON-EXISTENT-SKU");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetLowStockProductsAsync Tests

    /// <summary>Verifies that GetLowStockProductsAsync returns only products whose quantity in stock is below their reorder level.</summary>
    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsProductsBelowReorderLevel()
    {
        // Arrange
        CreateTestProduct("LOW-001", "Low Stock 1", quantityInStock: 5, reorderLevel: 10);
        CreateTestProduct("LOW-002", "Low Stock 2", quantityInStock: 3, reorderLevel: 10);
        CreateTestProduct("HIGH-001", "High Stock", quantityInStock: 100, reorderLevel: 10);

        // Act
        var result = await _repository.GetLowStockProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.True(p.QuantityInStock < p.ReorderLevel));
    }

    /// <summary>Verifies that GetLowStockProductsAsync returns an empty collection when all products have sufficient stock.</summary>
    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsEmpty_WhenNoLowStockProducts()
    {
        // Arrange
        CreateTestProduct("HIGH-001", "High Stock 1", quantityInStock: 100, reorderLevel: 10);
        CreateTestProduct("HIGH-002", "High Stock 2", quantityInStock: 200, reorderLevel: 10);

        // Act
        var result = await _repository.GetLowStockProductsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new product to the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidProduct_CreatesProduct()
    {
        // Arrange
        var product = new Product(Guid.NewGuid())
        {
            SKU = "NEW-SKU",
            Name = "New Product",
            Description = "New Description",
            UnitPrice = 50.00m,
            QuantityInStock = 75,
            ReorderLevel = 15
        };

        // Act
        await _repository.AddAsync(product);
        var result = await _context.Products.FindAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEW-SKU", result.SKU);
        Assert.Equal("New Product", result.Name);
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that UpdateAsync saves modified product fields to the database.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingProduct_UpdatesProductData()
    {
        // Arrange
        var product = CreateTestProduct("UPDATE-SKU", "Original Name");
        product.Name = "Updated Name";
        product.UnitPrice = 75.00m;

        // Act
        await _repository.UpdateAsync(product);
        var result = await _context.Products.FindAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(75.00m, result.UnitPrice);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that DeleteAsync removes the product from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesProduct()
    {
        // Arrange
        var product = CreateTestProduct("DELETE-SKU", "Delete Me");

        // Act
        await _repository.DeleteAsync(product);
        await _context.SaveChangesAsync();
        var result = await _context.Products.FindAsync(product.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that GetAllAsync returns all products currently in the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        // Arrange
        CreateTestProduct("LIST-001", "Product 1");
        CreateTestProduct("LIST-002", "Product 2");
        CreateTestProduct("LIST-003", "Product 3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that GetAllPaginatedAsync returns a correctly sized and numbered page of products.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        CreateTestProduct("PAGE-001", "Product 1");
        CreateTestProduct("PAGE-002", "Product 2");
        CreateTestProduct("PAGE-003", "Product 3");
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

    /// <summary>Verifies that GetAllPaginatedAsync returns items from the second page.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        CreateTestProduct("PAGE2-001", "Product 1");
        CreateTestProduct("PAGE2-002", "Product 2");
        CreateTestProduct("PAGE2-003", "Product 3");
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

    #region QueryAsync Tests

    /// <summary>Verifies that QueryAsync filters products by a search term applied to name and SKU fields.</summary>
    [Fact]
    public async Task QueryAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        CreateTestProduct("SEARCH-001", "Widget Product");
        CreateTestProduct("SEARCH-002", "Gadget Product");
        CreateTestProduct("OTHER-001", "Other Item");
        var querySpec = new QuerySpec { SearchTerm = "Widget" };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(p => p.Name.Contains("Widget", StringComparison.OrdinalIgnoreCase) ||
                                           p.SKU.Contains("Widget", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters products by a SKU filter and returns only matching results.</summary>
    [Fact]
    public async Task QueryAsync_WithSkuFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestProduct("FILTER-SKU-001", "Product 1");
        CreateTestProduct("FILTER-SKU-002", "Product 2");
        CreateTestProduct("OTHER-SKU", "Product 3");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "SKU", "FILTER-SKU" } };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(p => p.SKU.Contains("FILTER-SKU", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters products by a name filter and returns only matching results.</summary>
    [Fact]
    public async Task QueryAsync_WithNameFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestProduct("TEST-001", "Widget Product");
        CreateTestProduct("TEST-002", "Gadget Product");
        CreateTestProduct("TEST-003", "Other Item");
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "Name", "Widget" } };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().OnlyContain(p => p.Name.Contains("Widget", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies that QueryAsync filters products by a unit price range and returns only products within the range.</summary>
    [Fact]
    public async Task QueryAsync_WithPriceRangeFilter_ShouldFilterResults()
    {
        // Arrange
        CreateTestProduct("PRICE-001", "Cheap Product", unitPrice: 10.00m);
        CreateTestProduct("PRICE-002", "Mid Product", unitPrice: 50.00m);
        CreateTestProduct("PRICE-003", "Expensive Product", unitPrice: 100.00m);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string>
        {
            { "UnitPriceMin", "20" },
            { "UnitPriceMax", "75" }
        };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(p => p.UnitPrice >= 20m && p.UnitPrice <= 75m);
    }

    /// <summary>Verifies that QueryAsync returns the correct page of results when pagination is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        CreateTestProduct("PAGE-001", "Product 1");
        CreateTestProduct("PAGE-002", "Product 2");
        CreateTestProduct("PAGE-003", "Product 3");
        CreateTestProduct("PAGE-004", "Product 4");
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(4);
    }

    /// <summary>Verifies that QueryAsync returns products sorted in ascending order when a sort field is specified.</summary>
    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        CreateTestProduct("SORT-001", "Zebra Product");
        CreateTestProduct("SORT-002", "Alpha Product");
        CreateTestProduct("SORT-003", "Beta Product");
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = false };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var names = result.Items.Select(p => p.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        names.Should().BeEquivalentTo(sortedNames);
    }

    /// <summary>Verifies that QueryAsync returns products sorted in descending order when SortDesc is true.</summary>
    [Fact]
    public async Task QueryAsync_WithDescendingSort_ShouldReturnDescendingSortedResults()
    {
        // Arrange
        CreateTestProduct("SORT-DESC-001", "Alpha Product");
        CreateTestProduct("SORT-DESC-002", "Zebra Product");
        CreateTestProduct("SORT-DESC-003", "Beta Product");
        var querySpec = new QuerySpec { SortBy = "Name", SortDesc = true };
        var spec = new ProductQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var names = result.Items.Select(p => p.Name).ToList();
        var sortedNames = names.OrderByDescending(n => n).ToList();
        names.Should().BeEquivalentTo(sortedNames);
    }

    #endregion
}

