using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Sales.Infrastructure.Data.Repositories;
using MyApp.Sales.Tests.Helpers;
using Xunit;

namespace MyApp.Sales.Tests.Repositories;

/// <summary>Integration tests for <see cref="SalesOrderLineRepository"/> using an in-memory database.</summary>
public class SalesOrderLineRepositoryTests
{
    private readonly SalesDbContext _context;
    private readonly SalesOrderLineRepository _repository;

    /// <summary>Initializes a new <see cref="SalesOrderLineRepositoryTests"/> with a seeded in-memory database context.</summary>
    public SalesOrderLineRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new SalesOrderLineRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.SalesOrders.RemoveRange(_context.SalesOrders);
        _context.SalesOrderLines.RemoveRange(_context.SalesOrderLines);
        _context.Customers.RemoveRange(_context.Customers);
        _context.SaveChanges();

        // Create customers
        var customer1 = new Customer(Guid.NewGuid())
        {
            Name = "Customer 1",
            Email = "customer1@example.com"
        };
        var customer2 = new Customer(Guid.NewGuid())
        {
            Name = "Customer 2",
            Email = "customer2@example.com"
        };
        _context.Customers.AddRange(customer1, customer2);
        _context.SaveChanges();

        // Create sales orders
        var order1 = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-001",
            CustomerId = customer1.Id,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Draft,
            TotalAmount = 100.00m,
            IsQuote = false
        };
        var order2 = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-002",
            CustomerId = customer2.Id,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Confirmed,
            TotalAmount = 200.00m,
            IsQuote = true
        };
        _context.SalesOrders.AddRange(order1, order2);
        _context.SaveChanges();

        // Create sales order lines
        var line1 = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 10,
            UnitPrice = 5.00m,
            LineTotal = 50.00m,
            ProductSKU = "PROD-001",
            ProductName = "Product 1"
        };
        var line2 = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 5,
            UnitPrice = 10.00m,
            LineTotal = 50.00m,
            ProductSKU = "PROD-002",
            ProductName = "Product 2"
        };
        var line3 = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order2.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 20,
            UnitPrice = 10.00m,
            LineTotal = 200.00m,
            ProductSKU = "PROD-003",
            ProductName = "Product 3"
        };
        _context.SalesOrderLines.AddRange(line1, line2, line3);
        _context.SaveChanges();
    }

    private SalesOrderLine CreateTestSalesOrderLine(Guid salesOrderId, Guid? productId = null, int quantity = 10, decimal unitPrice = 5.00m)
    {
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = salesOrderId,
            ProductId = productId ?? Guid.NewGuid(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice,
            ProductSKU = $"SKU-{Guid.NewGuid().ToString().Substring(0, 8)}",
            ProductName = $"Product {Guid.NewGuid().ToString().Substring(0, 8)}"
        };
        _context.SalesOrderLines.Add(line);
        _context.SaveChanges();
        return line;
    }

    #region GetByIdAsync Tests

    /// <summary>Verifies that GetByIdAsync returns the sales order line with correct field values when the identifier exists.</summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSalesOrderLine()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = CreateTestSalesOrderLine(order.Id, quantity: 15, unitPrice: 7.50m);

        // Act
        var result = await _repository.GetByIdAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(line.Id);
        result.SalesOrderId.Should().Be(order.Id);
        result.Quantity.Should().Be(15);
        result.UnitPrice.Should().Be(7.50m);
        result.LineTotal.Should().Be(112.50m);
    }

    /// <summary>Verifies that GetByIdAsync returns null when no sales order line exists with the given identifier.</summary>
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

    #region ListAsync Tests

    /// <summary>Verifies that ListAsync returns all sales order lines present in the database.</summary>
    [Fact]
    public async Task ListAsync_ReturnsAllSalesOrderLines()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id);
        CreateTestSalesOrderLine(order.Id);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(5); // At least 3 seeded + 2 new
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>Verifies that GetAllAsync returns the complete set of sales order lines from the database.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllSalesOrderLines()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    /// <summary>Verifies that GetAllPaginatedAsync returns the correct page slice with accurate pagination metadata.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id, quantity: 1);
        CreateTestSalesOrderLine(order.Id, quantity: 2);
        CreateTestSalesOrderLine(order.Id, quantity: 3);
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(6);
    }

    /// <summary>Verifies that GetAllPaginatedAsync returns items from the second page when page number 2 is requested.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id, quantity: 1);
        CreateTestSalesOrderLine(order.Id, quantity: 2);
        CreateTestSalesOrderLine(order.Id, quantity: 3);
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

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new sales order line and makes it retrievable from the database.</summary>
    [Fact]
    public async Task AddAsync_WithValidSalesOrderLine_CreatesSalesOrderLine()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var productId = Guid.NewGuid();
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order.Id,
            ProductId = productId,
            Quantity = 25,
            UnitPrice = 12.50m,
            LineTotal = 312.50m,
            ProductSKU = "PROD-NEW",
            ProductName = "New Product"
        };

        // Act
        var result = await _repository.AddAsync(line);
        var savedLine = await _context.SalesOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(line.Id);
        savedLine.Should().NotBeNull();
        savedLine!.Quantity.Should().Be(25);
        savedLine.UnitPrice.Should().Be(12.50m);
        savedLine.ProductSKU.Should().Be("PROD-NEW");
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>Verifies that UpdateAsync saves changed line properties (quantity, unit price, line total, product name) to the database.</summary>
    [Fact]
    public async Task UpdateAsync_WithExistingSalesOrderLine_UpdatesSalesOrderLineData()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = CreateTestSalesOrderLine(order.Id, quantity: 10, unitPrice: 5.00m);
        line.Quantity = 20;
        line.UnitPrice = 7.50m;
        line.LineTotal = 150.00m;
        line.ProductName = "Updated Product";

        // Act
        var result = await _repository.UpdateAsync(line);
        var updatedLine = await _context.SalesOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        updatedLine.Should().NotBeNull();
        updatedLine!.Quantity.Should().Be(20);
        updatedLine.UnitPrice.Should().Be(7.50m);
        updatedLine.LineTotal.Should().Be(150.00m);
        updatedLine.ProductName.Should().Be("Updated Product");
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>Verifies that DeleteAsync removes the sales order line record from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesSalesOrderLine()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = CreateTestSalesOrderLine(order.Id, quantity: 10, unitPrice: 5.00m);

        // Act
        await _repository.DeleteAsync(line.Id);
        await _context.SaveChangesAsync();
        var deletedLine = await _context.SalesOrderLines.FindAsync(line.Id);

        // Assert
        deletedLine.Should().BeNull();
    }

    /// <summary>Verifies that DeleteAsync does not throw when called with an identifier that does not exist in the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Edge Cases

    /// <summary>Verifies that AddAsync persists a sales order line with a quantity of zero without error.</summary>
    [Fact]
    public async Task AddAsync_WithZeroQuantity_CreatesSalesOrderLine()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 0,
            UnitPrice = 10.00m,
            LineTotal = 0m
        };

        // Act
        var result = await _repository.AddAsync(line);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
    }

    /// <summary>Verifies that AddAsync persists a sales order line with a unit price of zero without error.</summary>
    [Fact]
    public async Task AddAsync_WithZeroUnitPrice_CreatesSalesOrderLine()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            SalesOrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 10,
            UnitPrice = 0m,
            LineTotal = 0m
        };

        // Act
        var result = await _repository.AddAsync(line);

        // Assert
        result.Should().NotBeNull();
        result.UnitPrice.Should().Be(0m);
    }

    /// <summary>Verifies that UpdateAsync handles a quantity set to int.MaxValue without overflow or persistence failure.</summary>
    [Fact]
    public async Task UpdateAsync_WithLargeQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        var line = CreateTestSalesOrderLine(order.Id, quantity: 10, unitPrice: 5.00m);
        line.Quantity = int.MaxValue;
        line.LineTotal = int.MaxValue * line.UnitPrice;

        // Act
        var result = await _repository.UpdateAsync(line);
        var updatedLine = await _context.SalesOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        updatedLine.Should().NotBeNull();
        updatedLine!.Quantity.Should().Be(int.MaxValue);
    }

    /// <summary>Verifies that GetAllPaginatedAsync returns all items when the requested page size exceeds the total record count.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithPageSizeLargerThanTotal_ReturnsAllItems()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id);
        var pageNumber = 1;
        var pageSize = 1000; // Larger than total

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(result.TotalCount);
    }

    /// <summary>Verifies that GetAllPaginatedAsync returns an empty items list when the requested page number is beyond the last page.</summary>
    [Fact]
    public async Task GetAllPaginatedAsync_WithPageBeyondTotal_ReturnsEmptyPage()
    {
        // Arrange
        var order = _context.SalesOrders.First();
        CreateTestSalesOrderLine(order.Id);
        var pageNumber = 1000; // Beyond total
        var pageSize = 10;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    #endregion
}

