using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Orders.Infrastructure.Repositories;
using MyApp.Orders.Tests.Helpers;
using Xunit;

namespace MyApp.Orders.Tests.Repositories;

public class ReservedStockRepositoryTests
{
    private readonly OrdersDbContext _context;
    private readonly ReservedStockRepository _repository;

    public ReservedStockRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new ReservedStockRepository(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _context.ReservedStocks.RemoveRange(_context.ReservedStocks);
        _context.SaveChanges();

        // Create test orders
        var order1 = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-001",
            Type = OrderType.Outbound,
            Status = OrderStatus.Draft
        };
        var order2 = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-002",
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved
        };
        _context.Orders.AddRange(order1, order2);
        _context.SaveChanges();

        // Create reserved stocks
        var reservation1 = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = order1.Id,
            Quantity = 10,
            ReservedUntil = DateTime.UtcNow.AddHours(24),
            Status = ReservationStatus.Reserved
        };
        var reservation2 = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = order1.Id,
            Quantity = 5,
            ReservedUntil = DateTime.UtcNow.AddHours(-1), // Expired
            Status = ReservationStatus.Reserved
        };
        var reservation3 = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = order2.Id,
            Quantity = 15,
            ReservedUntil = DateTime.UtcNow.AddHours(12),
            Status = ReservationStatus.Cancelled
        };
        _context.ReservedStocks.AddRange(reservation1, reservation2, reservation3);
        _context.SaveChanges();
    }

    private ReservedStock CreateTestReservation(Guid orderId, int quantity = 10, ReservationStatus status = ReservationStatus.Reserved)
    {
        var reservation = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = orderId,
            Quantity = quantity,
            ReservedUntil = DateTime.UtcNow.AddHours(24),
            Status = status
        };
        _context.ReservedStocks.Add(reservation);
        _context.SaveChanges();
        return reservation;
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsReservedStock()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = CreateTestReservation(orderId, 20);

        // Act
        var result = await _repository.GetByIdAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reservation.Id);
        result.Quantity.Should().Be(20);
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
    public async Task ListAsync_ReturnsAllReservations()
    {
        // Act
        var result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(3); // At least the seeded data
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidReservation_CreatesReservation()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = orderId,
            Quantity = 25,
            ReservedUntil = DateTime.UtcNow.AddHours(48),
            Status = ReservationStatus.Reserved
        };

        // Act
        await _repository.AddAsync(reservation);
        var savedReservation = await _context.ReservedStocks.FindAsync(reservation.Id);

        // Assert
        savedReservation.Should().NotBeNull();
        savedReservation!.Quantity.Should().Be(25);
        savedReservation.Status.Should().Be(ReservationStatus.Reserved);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingReservation_UpdatesReservation()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = CreateTestReservation(orderId, 10);
        reservation.Quantity = 30;
        reservation.Status = ReservationStatus.Cancelled;

        // Act
        await _repository.UpdateAsync(reservation);
        var updatedReservation = await _context.ReservedStocks.FindAsync(reservation.Id);

        // Assert
        updatedReservation.Should().NotBeNull();
        updatedReservation!.Quantity.Should().Be(30);
        updatedReservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesReservation()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = CreateTestReservation(orderId);

        // Act
        await _repository.DeleteAsync(reservation.Id);
        var deletedReservation = await _context.ReservedStocks.FindAsync(reservation.Id);

        // Assert
        deletedReservation.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetExpiredReservationsAsync Tests

    [Fact]
    public async Task GetExpiredReservationsAsync_ReturnsOnlyExpiredReservations()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        // Create an expired reservation
        var expiredReservation = new ReservedStock(Guid.NewGuid())
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OrderId = orderId,
            Quantity = 5,
            ReservedUntil = DateTime.UtcNow.AddHours(-2),
            Status = ReservationStatus.Reserved
        };
        _context.ReservedStocks.Add(expiredReservation);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetExpiredReservationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2); // At least the seeded expired one + the new one
        result.All(r => r.Status == ReservationStatus.Reserved).Should().BeTrue();
        result.All(r => r.ReservedUntil < DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task GetExpiredReservationsAsync_WithNoExpiredReservations_ReturnsEmptyList()
    {
        // Arrange
        // Clear all reservations
        _context.ReservedStocks.RemoveRange(_context.ReservedStocks);
        _context.SaveChanges();

        // Create only non-expired reservations
        var orderId = _context.Orders.First().Id;
        CreateTestReservation(orderId, 10, ReservationStatus.Reserved);

        // Act
        var result = await _repository.GetExpiredReservationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByOrderIdAsync Tests

    [Fact]
    public async Task GetByOrderIdAsync_WithExistingReservations_ReturnsAllReservationsForOrder()
    {
        // Arrange
        var order = _context.Orders.First();
        CreateTestReservation(order.Id, 20);
        CreateTestReservation(order.Id, 15);

        // Act
        var result = await _repository.GetByOrderIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(3); // At least 2 seeded + 2 new
        result.All(r => r.OrderId == order.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithNoReservations_ReturnsEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByIdWithDetailsAsync Tests

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithValidId_ReturnsReservedStock()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = CreateTestReservation(orderId, 25);

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reservation.Id);
        result.Quantity.Should().Be(25);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // Note: GetAllAsync and GetAllPaginatedAsync are not implemented in ReservedStockRepository
    // They are part of IRepository but ReservedStockRepository only implements custom methods
    // plus basic CRUD operations
}
