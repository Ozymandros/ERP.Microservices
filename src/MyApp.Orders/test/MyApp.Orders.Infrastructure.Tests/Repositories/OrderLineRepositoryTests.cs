using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Orders.Infrastructure.Repositories;
using MyApp.Orders.Tests.Helpers;
using Xunit;

namespace MyApp.Orders.Tests.Repositories;

public class OrderLineRepositoryTests
{
    private readonly OrdersDbContext _context;
    private readonly OrderLineRepository _repository;

    public OrderLineRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new OrderLineRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.Orders.RemoveRange(_context.Orders);
        _context.OrderLines.RemoveRange(_context.OrderLines);
        _context.SaveChanges();

        // Create orders
        var order1 = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-001",
            Type = OrderType.Inbound,
            Status = OrderStatus.Draft,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        var order2 = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-002",
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        _context.Orders.AddRange(order1, order2);
        _context.SaveChanges();

        // Create order lines
        var line1 = new OrderLine(Guid.NewGuid())
        {
            OrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 10,
            PickedQuantity = 0,
            ReservedQuantity = 0,
            IsFulfilled = false
        };
        var line2 = new OrderLine(Guid.NewGuid())
        {
            OrderId = order1.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 5,
            PickedQuantity = 0,
            ReservedQuantity = 0,
            IsFulfilled = false
        };
        var line3 = new OrderLine(Guid.NewGuid())
        {
            OrderId = order2.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 20,
            PickedQuantity = 20,
            ReservedQuantity = 20,
            ReservedStockId = Guid.NewGuid(),
            IsFulfilled = true
        };
        _context.OrderLines.AddRange(line1, line2, line3);
        _context.SaveChanges();
    }

    private OrderLine CreateTestOrderLine(Guid orderId, Guid? productId = null, int quantity = 10, int pickedQuantity = 0, int reservedQuantity = 0, bool isFulfilled = false)
    {
        var line = new OrderLine(Guid.NewGuid())
        {
            OrderId = orderId,
            ProductId = productId ?? Guid.NewGuid(),
            Quantity = quantity,
            PickedQuantity = pickedQuantity,
            ReservedQuantity = reservedQuantity,
            ReservedStockId = reservedQuantity > 0 ? Guid.NewGuid() : null,
            IsFulfilled = isFulfilled
        };
        _context.OrderLines.Add(line);
        _context.SaveChanges();
        return line;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrderLine()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 15, pickedQuantity: 5, reservedQuantity: 10);

        // Act
        var result = await _repository.GetByIdAsync(line.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(line.Id);
        result.OrderId.Should().Be(order.Id);
        result.Quantity.Should().Be(15);
        result.PickedQuantity.Should().Be(5);
        result.ReservedQuantity.Should().Be(10);
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

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsAllOrderLines()
    {
        // Arrange
        var order = _context.Orders.First();
        CreateTestOrderLine(order.Id);
        CreateTestOrderLine(order.Id);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(5); // At least 3 seeded + 2 new
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidOrderLine_CreatesOrderLine()
    {
        // Arrange
        var order = _context.Orders.First();
        var productId = Guid.NewGuid();
        var reservedStockId = Guid.NewGuid();
        var line = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = productId,
            Quantity = 25,
            PickedQuantity = 0,
            ReservedQuantity = 25,
            ReservedStockId = reservedStockId,
            IsFulfilled = false
        };

        // Act
        await _repository.AddAsync(line);
        var savedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        savedLine.Should().NotBeNull();
        savedLine!.Quantity.Should().Be(25);
        savedLine.ReservedQuantity.Should().Be(25);
        savedLine.ReservedStockId.Should().Be(reservedStockId);
        savedLine.IsFulfilled.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingOrderLine_UpdatesOrderLineData()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 10, pickedQuantity: 0, reservedQuantity: 0);
        line.Quantity = 20;
        line.PickedQuantity = 15;
        line.ReservedQuantity = 20;
        line.ReservedStockId = Guid.NewGuid();
        line.IsFulfilled = true;

        // Act
        await _repository.UpdateAsync(line);
        var updatedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        updatedLine.Should().NotBeNull();
        updatedLine!.Quantity.Should().Be(20);
        updatedLine.PickedQuantity.Should().Be(15);
        updatedLine.ReservedQuantity.Should().Be(20);
        updatedLine.IsFulfilled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithFulfillment_UpdatesIsFulfilled()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 10, pickedQuantity: 0, reservedQuantity: 10, isFulfilled: false);
        line.PickedQuantity = 10;
        line.IsFulfilled = true;

        // Act
        await _repository.UpdateAsync(line);
        var updatedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        updatedLine.Should().NotBeNull();
        updatedLine!.PickedQuantity.Should().Be(10);
        updatedLine.IsFulfilled.Should().BeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesOrderLine()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 10);

        // Act
        await _repository.DeleteAsync(line.Id);
        var deletedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        deletedLine.Should().BeNull();
    }

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

    [Fact]
    public async Task AddAsync_WithZeroQuantity_CreatesOrderLine()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 0,
            PickedQuantity = 0,
            ReservedQuantity = 0,
            IsFulfilled = true
        };

        // Act
        await _repository.AddAsync(line);
        var savedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        savedLine.Should().NotBeNull();
        savedLine!.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_WithReservedStockId_CreatesOrderLineWithReservation()
    {
        // Arrange
        var order = _context.Orders.First();
        var reservedStockId = Guid.NewGuid();
        var line = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 10,
            PickedQuantity = 0,
            ReservedQuantity = 10,
            ReservedStockId = reservedStockId,
            IsFulfilled = false
        };

        // Act
        await _repository.AddAsync(line);
        var savedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        savedLine.Should().NotBeNull();
        savedLine!.ReservedStockId.Should().Be(reservedStockId);
        savedLine.ReservedQuantity.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_WithPickedQuantityExceedingQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 10, pickedQuantity: 0);
        line.PickedQuantity = 15; // Exceeds quantity (over-pick scenario)

        // Act
        await _repository.UpdateAsync(line);
        var updatedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        updatedLine.Should().NotBeNull();
        updatedLine!.PickedQuantity.Should().Be(15);
    }

    [Fact]
    public async Task UpdateAsync_WithReservedQuantityExceedingQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var order = _context.Orders.First();
        var line = CreateTestOrderLine(order.Id, quantity: 10, reservedQuantity: 0);
        line.ReservedQuantity = 15; // Exceeds quantity

        // Act
        await _repository.UpdateAsync(line);
        var updatedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        updatedLine.Should().NotBeNull();
        updatedLine!.ReservedQuantity.Should().Be(15);
    }

    [Fact]
    public async Task UpdateAsync_WithNullReservedStockId_UpdatesSuccessfully()
    {
        // Arrange
        var order = _context.Orders.First();
        var reservedStockId = Guid.NewGuid();
        var line = CreateTestOrderLine(order.Id, quantity: 10, reservedQuantity: 10);
        line.ReservedStockId = reservedStockId;
        await _repository.UpdateAsync(line);
        
        // Now remove the reservation
        line.ReservedStockId = null;
        line.ReservedQuantity = 0;

        // Act
        await _repository.UpdateAsync(line);
        var updatedLine = await _context.OrderLines.FindAsync(line.Id);

        // Assert
        updatedLine.Should().NotBeNull();
        updatedLine!.ReservedStockId.Should().BeNull();
        updatedLine.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_WithFulfilledAndUnfulfilledLines_ReturnsAllLines()
    {
        // Arrange
        var order = _context.Orders.First();
        CreateTestOrderLine(order.Id, quantity: 10, isFulfilled: true);
        CreateTestOrderLine(order.Id, quantity: 20, isFulfilled: false);

        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(5);
        result.Any(l => l.IsFulfilled).Should().BeTrue();
        result.Any(l => !l.IsFulfilled).Should().BeTrue();
    }

    #endregion
}
