using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Inventory.Infrastructure.Data.Repositories;
using MyApp.Inventory.Tests.Helpers;
using Xunit;

namespace MyApp.Inventory.Tests.Repositories;

public class ProductRepositoryTests
{
    private readonly InventoryDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new ProductRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Product CreateTestProduct(string sku = "TEST-001", string name = "Test Product", int quantityInStock = 100, int reorderLevel = 10)
    {
        var product = new Product(Guid.NewGuid())
        {
            SKU = sku,
            Name = name,
            Description = "Test Description",
            UnitPrice = 25.00m,
            QuantityInStock = quantityInStock,
            ReorderLevel = reorderLevel
        };
        _context.Products.Add(product);
        _context.SaveChanges();
        return product;
    }

    #region GetByIdAsync Tests

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

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesProduct()
    {
        // Arrange
        var product = CreateTestProduct("DELETE-SKU", "Delete Me");

        // Act
        await _repository.DeleteAsync(product);
        var result = await _context.Products.FindAsync(product.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

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
}
