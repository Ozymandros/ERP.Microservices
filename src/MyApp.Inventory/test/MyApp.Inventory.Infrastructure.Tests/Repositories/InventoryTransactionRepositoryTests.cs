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

public class InventoryTransactionRepositoryTests
{
    private readonly InventoryDbContext _context;
    private readonly InventoryTransactionRepository _repository;

    public InventoryTransactionRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new InventoryTransactionRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.InventoryTransactions.RemoveRange(_context.InventoryTransactions);
        _context.Products.RemoveRange(_context.Products);
        _context.Warehouses.RemoveRange(_context.Warehouses);
        _context.SaveChanges();

        // Create products
        var product1 = new Product(Guid.NewGuid())
        {
            SKU = "PROD-001",
            Name = "Product 1"
        };
        var product2 = new Product(Guid.NewGuid())
        {
            SKU = "PROD-002",
            Name = "Product 2"
        };
        _context.Products.AddRange(product1, product2);

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
        _context.SaveChanges();

        // Create transactions
        var transaction1 = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = product1.Id,
            WarehouseId = warehouse1.Id,
            QuantityChange = 100,
            TransactionType = TransactionType.Inbound,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = "REF-001"
        };
        var transaction2 = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = product1.Id,
            WarehouseId = warehouse2.Id,
            QuantityChange = -50,
            TransactionType = TransactionType.Outbound,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = "REF-002"
        };
        var transaction3 = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = product2.Id,
            WarehouseId = warehouse1.Id,
            QuantityChange = 25,
            TransactionType = TransactionType.Adjustment,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = "REF-003"
        };
        _context.InventoryTransactions.AddRange(transaction1, transaction2, transaction3);
        _context.SaveChanges();
    }

    private InventoryTransaction CreateTestTransaction(Guid productId, Guid warehouseId, int quantityChange = 10, TransactionType type = TransactionType.Inbound)
    {
        var transaction = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            QuantityChange = quantityChange,
            TransactionType = type,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = $"REF-{Guid.NewGuid()}"
        };
        _context.InventoryTransactions.Add(transaction);
        _context.SaveChanges();
        return transaction;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsInventoryTransaction()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var transaction = CreateTestTransaction(product.Id, warehouse.Id, 75);

        // Act
        var result = await _repository.GetByIdAsync(transaction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
        result.QuantityChange.Should().Be(75);
    }

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

    [Fact]
    public async Task GetAllAsync_ReturnsAllTransactions()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least the seeded data
    }

    #endregion

    #region GetAllPaginatedAsync Tests

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

    #endregion

    #region GetByProductIdAsync Tests

    [Fact]
    public async Task GetByProductIdAsync_WithExistingTransactions_ReturnsAllTransactionsForProduct()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 20);
        CreateTestTransaction(product.Id, warehouse.Id, 30);

        // Act
        var result = await _repository.GetByProductIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least 1 seeded + 2 new
        result.All(t => t.ProductId == product.Id).Should().BeTrue();
        result.All(t => t.Product != null).Should().BeTrue();
        result.All(t => t.Warehouse != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByProductIdAsync_WithNoTransactions_ReturnsEmptyList()
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

    [Fact]
    public async Task GetByWarehouseIdAsync_WithExistingTransactions_ReturnsAllTransactionsForWarehouse()
    {
        // Arrange
        var warehouse = _context.Warehouses.First();
        var product1 = _context.Products.First();
        var product2 = _context.Products.Skip(1).First();
        CreateTestTransaction(product1.Id, warehouse.Id, 15);
        CreateTestTransaction(product2.Id, warehouse.Id, 20);

        // Act
        var result = await _repository.GetByWarehouseIdAsync(warehouse.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2); // At least 1 seeded + 2 new
        result.All(t => t.WarehouseId == warehouse.Id).Should().BeTrue();
        result.All(t => t.Product != null).Should().BeTrue();
        result.All(t => t.Warehouse != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByWarehouseIdAsync_WithNoTransactions_ReturnsEmptyList()
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

    #region GetByTransactionTypeAsync Tests

    [Fact]
    public async Task GetByTransactionTypeAsync_WithExistingTransactions_ReturnsTransactionsOfType()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 50, TransactionType.Inbound);
        CreateTestTransaction(product.Id, warehouse.Id, -25, TransactionType.Outbound);

        // Act
        var result = await _repository.GetByTransactionTypeAsync(TransactionType.Inbound);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(2); // At least 1 seeded + 1 new
        result.All(t => t.TransactionType == TransactionType.Inbound).Should().BeTrue();
        result.All(t => t.Product != null).Should().BeTrue();
        result.All(t => t.Warehouse != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTransactionTypeAsync_WithNoTransactionsOfType_ReturnsEmptyList()
    {
        // Arrange
        // Clear all transactions
        _context.InventoryTransactions.RemoveRange(_context.InventoryTransactions);
        _context.SaveChanges();

        // Create only Outbound transactions
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, -10, TransactionType.Outbound);

        // Act
        var result = await _repository.GetByTransactionTypeAsync(TransactionType.Inbound);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidTransaction_CreatesTransaction()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var transaction = new InventoryTransaction(Guid.NewGuid())
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            QuantityChange = 200,
            TransactionType = TransactionType.Inbound,
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = "REF-NEW"
        };

        // Act
        var result = await _repository.AddAsync(transaction);
        var savedTransaction = await _context.InventoryTransactions.FindAsync(transaction.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.QuantityChange.Should().Be(200);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingTransaction_UpdatesTransactionData()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var transaction = CreateTestTransaction(product.Id, warehouse.Id, 10);
        transaction.QuantityChange = 50;
        transaction.TransactionType = TransactionType.Adjustment;

        // Act
        var result = await _repository.UpdateAsync(transaction);
        var updatedTransaction = await _context.InventoryTransactions.FindAsync(transaction.Id);

        // Assert
        result.Should().NotBeNull();
        updatedTransaction.Should().NotBeNull();
        updatedTransaction!.QuantityChange.Should().Be(50);
        updatedTransaction.TransactionType.Should().Be(TransactionType.Adjustment);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidTransaction_DeletesTransaction()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        var transaction = CreateTestTransaction(product.Id, warehouse.Id, 10);

        // Act
        await _repository.DeleteAsync(transaction);
        await _context.SaveChangesAsync();
        var deletedTransaction = await _context.InventoryTransactions.FindAsync(transaction.Id);

        // Assert
        deletedTransaction.Should().BeNull();
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithTransactionTypeFilter_ShouldFilterResults()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 50, TransactionType.Inbound);
        CreateTestTransaction(product.Id, warehouse.Id, -25, TransactionType.Outbound);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "TransactionType", TransactionType.Inbound.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(t => t.TransactionType == TransactionType.Inbound);
    }

    [Fact]
    public async Task QueryAsync_WithProductIdFilter_ShouldFilterResults()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 30);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "ProductId", product.Id.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(t => t.ProductId == product.Id);
    }

    [Fact]
    public async Task QueryAsync_WithWarehouseIdFilter_ShouldFilterResults()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 40);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "WarehouseId", warehouse.Id.ToString() } };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(t => t.WarehouseId == warehouse.Id);
    }

    [Fact]
    public async Task QueryAsync_WithQuantityRangeFilter_ShouldFilterResults()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 30);
        CreateTestTransaction(product.Id, warehouse.Id, 100);
        CreateTestTransaction(product.Id, warehouse.Id, 5);
        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string>
        {
            { "QuantityChangeMin", "20" },
            { "QuantityChangeMax", "50" }
        };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(t => t.QuantityChange >= 20 && t.QuantityChange <= 50);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 10);
        CreateTestTransaction(product.Id, warehouse.Id, 20);
        CreateTestTransaction(product.Id, warehouse.Id, 30);
        CreateTestTransaction(product.Id, warehouse.Id, 40);
        var querySpec = new QuerySpec { Page = 2, PageSize = 2 };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(7);
    }

    [Fact]
    public async Task QueryAsync_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var product = _context.Products.First();
        var warehouse = _context.Warehouses.First();
        CreateTestTransaction(product.Id, warehouse.Id, 100);
        CreateTestTransaction(product.Id, warehouse.Id, 10);
        CreateTestTransaction(product.Id, warehouse.Id, 50);
        var querySpec = new QuerySpec { SortBy = "QuantityChange", SortDesc = false };
        var spec = new InventoryTransactionQuerySpec(querySpec);

        // Act
        var result = await _repository.QueryAsync(spec);

        // Assert
        result.Should().NotBeNull();
        var quantities = result.Items.Select(t => t.QuantityChange).ToList();
        var sortedQuantities = quantities.OrderBy(q => q).ToList();
        quantities.Should().BeEquivalentTo(sortedQuantities);
    }

    #endregion
}

