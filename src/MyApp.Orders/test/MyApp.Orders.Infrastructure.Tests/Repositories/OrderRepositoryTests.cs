using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Orders.Infrastructure.Repositories;
using MyApp.Orders.Tests.Helpers;
using Xunit;

namespace MyApp.Orders.Tests.Repositories;

public class OrderRepositoryTests
{
    private readonly OrdersDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new OrderRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Order CreateTestOrder(string orderNumber = "ORD-001")
    {
        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            Type = OrderType.Transfer,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };
        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder("ORD-001");

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("ORD-001", result.OrderNumber);
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

    [Fact]
    public async Task GetByIdAsync_IncludesOrderLines()
    {
        // Arrange
        var order = CreateTestOrder("ORD-002");
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };
        _context.OrderLines.Add(orderLine);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Lines);
        Assert.Single(result.Lines);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsAllOrders()
    {
        // Arrange
        CreateTestOrder("ORD-003");
        CreateTestOrder("ORD-004");
        CreateTestOrder("ORD-005");

        // Act
        var result = await _repository.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoOrders()
    {
        // Arrange
        _context.Orders.RemoveRange(_context.Orders);
        _context.SaveChanges();

        // Act
        var result = await _repository.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidOrder_CreatesOrder()
    {
        // Arrange
        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-NEW",
            Type = OrderType.Inbound,
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        // Act
        await _repository.AddAsync(order);
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORD-NEW", result.OrderNumber);
        Assert.Equal(OrderType.Inbound, result.Type);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingOrder_UpdatesOrderData()
    {
        // Arrange
        var order = CreateTestOrder("ORD-UPDATE");
        order.Status = OrderStatus.Approved;
        order.Type = OrderType.Outbound;

        // Act
        await _repository.UpdateAsync(order);
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Approved, result.Status);
        Assert.Equal(OrderType.Outbound, result.Type);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesOrder()
    {
        // Arrange
        var order = CreateTestOrder("ORD-DELETE");

        // Act
        await _repository.DeleteAsync(order.Id);
        var result = await _context.Orders.FindAsync(order.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await _repository.DeleteAsync(nonExistentId); // Should not throw
    }

    #endregion
}
