using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Inventory.Infrastructure.Repositories;
using MyApp.Inventory.Tests.Helpers;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Inventory.Tests.Repositories;

/// <summary>Integration tests for <see cref="WarehouseStockRepository"/> using an in-memory EF Core database.</summary>
public class WarehouseStockRepositoryTests
{
    private readonly InventoryDbContext _context;
    private readonly WarehouseStockRepository _repository;

    /// <summary>Initialises the in-memory database context and repository, and seeds base stock data before each test.</summary>
    public WarehouseStockRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new WarehouseStockRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.WarehouseStocks.RemoveRange(_context.WarehouseStocks);
        _context.Products.RemoveRange(_context.Products);
        _context.Warehouses.RemoveRange(_context.Warehouses);
        _context.SaveChanges();

        // Create warehouses
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

        // Create products
        var product1 = new Product(Guid.NewGuid())
        {
            SKU = "PROD-001",
            Name = "Product 1",
            ReorderLevel = 10
        };
        var product2 = new Product(Guid.NewGuid())
        {
            SKU = "PROD-002",
            Name = "Product 2",
            ReorderLevel = 5
        };
        _context.Products.AddRange(product1, product2);
        _context.SaveChanges();

        // Create warehouse stocks
        var stock1 = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = product1.Id,
            WarehouseId = warehouse1.Id,
            AvailableQuantity = 100,
            ReservedQuantity = 10,
            OnOrderQuantity = 5
        };
        var stock2 = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = product1.Id,
            WarehouseId = warehouse2.Id,
            AvailableQuantity = 50,
            ReservedQuantity = 5,
            OnOrderQuantity = 2
        };
        var stock3 = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = product2.Id,
            WarehouseId = warehouse1.Id,
            AvailableQuantity = 3, // Low stock
            ReservedQuantity = 0,
            OnOrderQuantity = 0
        };
        _context.WarehouseStocks.AddRange(stock1, stock2, stock3);
        _context.SaveChanges();
    }

    private WarehouseStock CreateTestWarehouseStock(Guid productId, Guid warehouseId, int availableQuantity = 100)
    {
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = availableQuantity,
            ReservedQuantity = 0,
            OnOrderQuantity = 0
        };
        _context.WarehouseStocks.Add(stock);
        _context.SaveChanges();
        return stock;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that GetByIdAsync returns the warehouse stock record when a valid ID is provided.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWarehouseStock()
    {
        // Arrange
        var productId = _context.Products.First().Id;
        var warehouseId = _context.Warehouses.First().Id;
        var stock = CreateTestWarehouseStock(productId, warehouseId, 75);

        // Act
        var result = await _repository.GetByIdAsync(stock.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(stock.Id);
        result.AvailableQuantity.Should().Be(75);
    }

    /// <summary>Verifies that GetByIdAsync returns null when no stock record with the given ID exists.</summary>
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

    /// <summary>Verifies that GetAllAsync returns all warehouse stock records currently in the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllWarehouseStocks()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least the seeded data
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that GetAllPaginatedAsync returns a correctly sized and numbered page of stock records.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
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

    /// <summary>Verifies that GetAllPaginatedAsync returns items from the second page when requested.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
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

    #region GetByProductAndWarehouseAsync Tests

    /// <summary>Verifies that GetByProductAndWarehouseAsync returns the stock record with navigation properties when a matching combination exists.</summary>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_WithExistingStock_ReturnsWarehouseStock()
    {
        // Arrange
        var product = new Product(Guid.NewGuid())
        {
            SKU = "PROD-TEST",
            Name = "Test Product",
            ReorderLevel = 10
        };
        var warehouse = new Warehouse(Guid.NewGuid())
        {
            Name = "Test Warehouse",
            Location = "Test Location"
        };
        _context.Products.Add(product);
        _context.Warehouses.Add(warehouse);
        _context.SaveChanges();

        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            AvailableQuantity = 150,
            ReservedQuantity = 0,
            OnOrderQuantity = 0
        };
        _context.WarehouseStocks.Add(stock);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByProductAndWarehouseAsync(product.Id, warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(product.Id);
        result.WarehouseId.Should().Be(warehouse.Id);
        result.AvailableQuantity.Should().Be(150);
        result.Product.Should().NotBeNull();
        result.Warehouse.Should().NotBeNull();
    }

    /// <summary>Verifies that GetByProductAndWarehouseAsync returns null when no stock record exists for the given product and warehouse combination.</summary>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_WithNonExistentStock_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByProductAndWarehouseAsync(productId, warehouseId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByProductIdAsync Tests

    /// <summary>Verifies that GetByProductIdAsync returns all stock records for the specified product, including navigation properties.</summary>
    [Fact]
    public async Task GetByProductIdAsync_WithExistingStocks_ReturnsAllStocksForProduct()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse1 = _context.Warehouses.First();
        var warehouse2 = _context.Warehouses.Skip(1).First();
        CreateTestWarehouseStock(product.Id, warehouse1.Id, 100);
        CreateTestWarehouseStock(product.Id, warehouse2.Id, 200);

        // Act
        var result = await _repository.GetByProductIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.All(s => s.ProductId == product.Id).Should().BeTrue();
        result.All(s => s.Product != null).Should().BeTrue();
    }

    /// <summary>Verifies that GetByProductIdAsync returns an empty list when no stock records exist for the given product ID.</summary>
    [Fact]
    public async Task GetByProductIdAsync_WithNoStocks_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByWarehouseIdAsync Tests

    /// <summary>Verifies that GetByWarehouseIdAsync returns all stock records for the specified warehouse, including navigation properties.</summary>
    [Fact]
    public async Task GetByWarehouseIdAsync_WithExistingStocks_ReturnsAllStocksForWarehouse()
    {
        // Arrange
        var warehouse = _context.Warehouses.First();
        var product1 = _context.Products.First();
        var product2 = _context.Products.Skip(1).First();
        CreateTestWarehouseStock(product1.Id, warehouse.Id, 50);
        CreateTestWarehouseStock(product2.Id, warehouse.Id, 75);

        // Act
        var result = await _repository.GetByWarehouseIdAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.All(s => s.WarehouseId == warehouse.Id).Should().BeTrue();
        result.All(s => s.Warehouse != null).Should().BeTrue();
    }

    /// <summary>Verifies that GetByWarehouseIdAsync returns an empty list when no stock records exist for the given warehouse ID.</summary>
    [Fact]
    public async Task GetByWarehouseIdAsync_WithNoStocks_ReturnsEmptyList()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByWarehouseIdAsync(warehouseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetLowStockAsync Tests

    /// <summary>Verifies that GetLowStockAsync returns stock records where available quantity is at or below each product's reorder level.</summary>
    [Fact]
    public async Task GetLowStockAsync_WithDefaultReorderLevel_ReturnsLowStockItems()
    {
        // Act
        var result = await _repository.GetLowStockAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(s => s.AvailableQuantity <= s.Product!.ReorderLevel);
    }

    /// <summary>Verifies that GetLowStockAsync returns only stock records with available quantity at or below the custom reorder level.</summary>
    [Fact]
    public async Task GetLowStockAsync_WithCustomReorderLevel_ReturnsItemsBelowLevel()
    {
        // Arrange
        var customReorderLevel = 20;

        // Act
        var result = await _repository.GetLowStockAsync(customReorderLevel);

        // Assert
        result.Should().NotBeNull();
        result.All(s => s.AvailableQuantity <= customReorderLevel).Should().BeTrue();
    }

    /// <summary>Verifies that GetLowStockAsync returns a greater number of items when a high reorder level threshold is specified.</summary>
    [Fact]
    public async Task GetLowStockAsync_WithHighReorderLevel_ReturnsMoreItems()
    {
        // Arrange
        var highReorderLevel = 200;

        // Act
        var result = await _repository.GetLowStockAsync(highReorderLevel);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // All stocks should be below 200
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new warehouse stock record to the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidWarehouseStock_CreatesStock()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            AvailableQuantity = 200,
            ReservedQuantity = 0,
            OnOrderQuantity = 0
        };

        // Act
        var result = await _repository.AddAsync(stock);
        var savedStock = await _context.WarehouseStocks.FindAsync(stock.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(stock.Id);
        savedStock.Should().NotBeNull();
        savedStock!.AvailableQuantity.Should().Be(200);
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that UpdateAsync saves modified stock quantity fields to the database.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingStock_UpdatesStockData()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var stock = CreateTestWarehouseStock(product.Id, warehouse.Id, 100);
        stock.AvailableQuantity = 150;
        stock.ReservedQuantity = 25;

        // Act
        var result = await _repository.UpdateAsync(stock);
        var updatedStock = await _context.WarehouseStocks.FindAsync(stock.Id);

        // Assert
        result.Should().NotBeNull();
        updatedStock.Should().NotBeNull();
        updatedStock!.AvailableQuantity.Should().Be(150);
        updatedStock.ReservedQuantity.Should().Be(25);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that DeleteAsync removes the warehouse stock record from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidStock_DeletesStock()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var stock = CreateTestWarehouseStock(product.Id, warehouse.Id, 100);

        // Act
        await _repository.DeleteAsync(stock);
        await _context.SaveChangesAsync();
        var deletedStock = await _context.WarehouseStocks.FindAsync(stock.Id);

        // Assert
        deletedStock.Should().BeNull();
    }

    #endregion

    // Note: QueryAsync tests will be added when WarehouseStockQuerySpec is created
    // This is covered in the query-spec-tests task
}

