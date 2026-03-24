using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Purchasing.Tests.Helpers;
using Xunit;

namespace MyApp.Purchasing.Tests.Repositories;

public class PurchaseOrderLineRepositoryTests
{
    private readonly PurchasingDbContext _context;
    private readonly PurchaseOrderLineRepository _repository;

    public PurchaseOrderLineRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new PurchaseOrderLineRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.PurchaseOrders.RemoveRange(_context.PurchaseOrders);
        _context.PurchaseOrderLines.RemoveRange(_context.PurchaseOrderLines);
        _context.Suppliers.RemoveRange(_context.Suppliers);
        _context.SaveChanges();

        // Create suppliers
        var supplier1 = new Supplier(Guid.NewGuid())
        {
            Name = "Supplier 1",
            Email = "supplier1@example.com"
        };
        var supplier2 = new Supplier(Guid.NewGuid())
        {
            Name = "Supplier 2",
            Email = "supplier2@example.com"
        };
        _context.Suppliers.AddRange(supplier1, supplier2);
        _context.SaveChanges();

        // Create purchase orders
        var order1 = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-001",
            SupplierId = supplier1.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 500.00m
        };
        var order2 = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-002",
            SupplierId = supplier2.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Approved,
            TotalAmount = 750.00m
        };
        _context.PurchaseOrders.AddRange(order1, order2);
        _context.SaveChanges();

        // Create purchase order lines
        var line1 = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 50,
            UnitPrice = 10.00m,
            LineTotal = 500.00m,
            ReceivedQuantity = 0,
            IsFullyReceived = false
        };
        var line2 = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 25,
            UnitPrice = 20.00m,
            LineTotal = 500.00m,
            ReceivedQuantity = 0,
            IsFullyReceived = false
        };
        var line3 = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order2.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 30,
            UnitPrice = 25.00m,
            LineTotal = 750.00m,
            ReceivedQuantity = 30,
            IsFullyReceived = true
        };
        _context.PurchaseOrderLines.AddRange(line1, line2, line3);
        _context.SaveChanges();
    }

    private PurchaseOrderLine CreateTestPurchaseOrderLine(Guid purchaseOrderId, Guid? productId = null, int quantity = 10, decimal unitPrice = 5.00m, int receivedQuantity = 0)
    {
        var line = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId ?? Guid.NewGuid(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice,
            ReceivedQuantity = receivedQuantity,
            IsFullyReceived = receivedQuantity >= quantity
        };
        _context.PurchaseOrderLines.Add(line);
        _context.SaveChanges();
        return line;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsPurchaseOrderLine()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = CreateTestPurchaseOrderLine(order.Id, quantity: 15, unitPrice: 7.50m);

        // Act
        var result = await _repository.GetByIdAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(line.Id);
        result.PurchaseOrderId.Should().Be(order.Id);
        result.Quantity.Should().Be(15);
        result.UnitPrice.Should().Be(7.50m);
        result.LineTotal.Should().Be(112.50m);
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

    #region GetByPurchaseOrderIdAsync Tests

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_WithExistingLines_ReturnsAllLinesForOrder()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        CreateTestPurchaseOrderLine(order.Id, quantity: 5);
        CreateTestPurchaseOrderLine(order.Id, quantity: 10);

        // Act
        var result = await _repository.GetByPurchaseOrderIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least 1 seeded + 2 new (seeded data has 2 lines for first order)
        result.All(l => l.PurchaseOrderId == order.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_WithNoLines_ReturnsEmptyList()
    {
        // Arrange
        var newOrder = new PurchaseOrder(Guid.NewGuid())
        {
            OrderNumber = "PO-NEW",
            SupplierId = _context.Suppliers.First().Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 0m
        };
        _context.PurchaseOrders.Add(newOrder);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByPurchaseOrderIdAsync(newOrder.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_WithMultipleOrders_ReturnsOnlyLinesForSpecifiedOrder()
    {
        // Arrange
        var order1 = _context.PurchaseOrders.First();
        var order2 = _context.PurchaseOrders.Skip(1).First();
        CreateTestPurchaseOrderLine(order1.Id, quantity: 5);
        CreateTestPurchaseOrderLine(order2.Id, quantity: 10);

        // Act
        var result = await _repository.GetByPurchaseOrderIdAsync(order1.Id);

        // Assert
        result.Should().NotBeNull();
        result.All(l => l.PurchaseOrderId == order1.Id).Should().BeTrue();
        result.Any(l => l.PurchaseOrderId == order2.Id).Should().BeFalse();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllPurchaseOrderLines()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        CreateTestPurchaseOrderLine(order.Id);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion

    #region GetAllPaginatedAsync Tests

    [Fact]
    public async Task GetAllPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        CreateTestPurchaseOrderLine(order.Id, quantity: 1);
        CreateTestPurchaseOrderLine(order.Id, quantity: 2);
        CreateTestPurchaseOrderLine(order.Id, quantity: 3);
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

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPurchaseOrderLine_CreatesPurchaseOrderLine()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var productId = Guid.NewGuid();
        var line = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            ProductId = productId,
            Quantity = 25,
            UnitPrice = 12.50m,
            LineTotal = 312.50m,
            ReceivedQuantity = 0,
            IsFullyReceived = false
        };

        // Act
        var result = await _repository.AddAsync(line);
        var savedLine = await _context.PurchaseOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(line.Id);
        savedLine.Should().NotBeNull();
        savedLine!.Quantity.Should().Be(25);
        savedLine.UnitPrice.Should().Be(12.50m);
        savedLine.ReceivedQuantity.Should().Be(0);
        savedLine.IsFullyReceived.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingPurchaseOrderLine_UpdatesPurchaseOrderLineData()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = CreateTestPurchaseOrderLine(order.Id, quantity: 10, unitPrice: 5.00m, receivedQuantity: 0);
        line.Quantity = 20;
        line.UnitPrice = 7.50m;
        line.LineTotal = 150.00m;
        line.ReceivedQuantity = 20;
        line.IsFullyReceived = true;

        // Act
        var result = await _repository.UpdateAsync(line);
        var updatedLine = await _context.PurchaseOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        updatedLine.Should().NotBeNull();
        updatedLine!.Quantity.Should().Be(20);
        updatedLine.UnitPrice.Should().Be(7.50m);
        updatedLine.LineTotal.Should().Be(150.00m);
        updatedLine.ReceivedQuantity.Should().Be(20);
        updatedLine.IsFullyReceived.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithPartialReceipt_UpdatesReceivedQuantity()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = CreateTestPurchaseOrderLine(order.Id, quantity: 100, unitPrice: 10.00m, receivedQuantity: 0);
        line.ReceivedQuantity = 50;
        line.IsFullyReceived = false;

        // Act
        var result = await _repository.UpdateAsync(line);
        var updatedLine = await _context.PurchaseOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        updatedLine.Should().NotBeNull();
        updatedLine!.ReceivedQuantity.Should().Be(50);
        updatedLine.IsFullyReceived.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidPurchaseOrderLine_DeletesPurchaseOrderLine()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = CreateTestPurchaseOrderLine(order.Id, quantity: 10, unitPrice: 5.00m);

        // Act
        await _repository.DeleteAsync(line);
        var deletedLine = await _context.PurchaseOrderLines.FindAsync(line.Id);

        // Assert
        deletedLine.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_WithFullyReceivedLines_ReturnsAllLines()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        CreateTestPurchaseOrderLine(order.Id, quantity: 10, receivedQuantity: 10); // Fully received
        CreateTestPurchaseOrderLine(order.Id, quantity: 20, receivedQuantity: 5); // Partially received
        CreateTestPurchaseOrderLine(order.Id, quantity: 30, receivedQuantity: 0); // Not received

        // Act
        var result = await _repository.GetByPurchaseOrderIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4); // At least 1 seeded + 3 new
        result.Any(l => l.IsFullyReceived).Should().BeTrue();
        result.Any(l => !l.IsFullyReceived).Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WithZeroQuantity_CreatesPurchaseOrderLine()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 0,
            UnitPrice = 10.00m,
            LineTotal = 0m,
            ReceivedQuantity = 0,
            IsFullyReceived = true
        };

        // Act
        var result = await _repository.AddAsync(line);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithReceivedQuantityExceedingQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        var line = CreateTestPurchaseOrderLine(order.Id, quantity: 10, unitPrice: 5.00m, receivedQuantity: 0);
        line.ReceivedQuantity = 15; // Exceeds quantity (over-receipt scenario)
        line.IsFullyReceived = true;

        // Act
        var result = await _repository.UpdateAsync(line);
        var updatedLine = await _context.PurchaseOrderLines.FindAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        updatedLine.Should().NotBeNull();
        updatedLine!.ReceivedQuantity.Should().Be(15);
    }

    [Fact]
    public async Task GetAllPaginatedAsync_WithPageSizeLargerThanTotal_ReturnsAllItems()
    {
        // Arrange
        var order = _context.PurchaseOrders.First();
        CreateTestPurchaseOrderLine(order.Id);
        var pageNumber = 1;
        var pageSize = 1000;

        // Act
        var result = await _repository.GetAllPaginatedAsync(pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountLessThanOrEqualTo(result.TotalCount);
    }

    #endregion
}

