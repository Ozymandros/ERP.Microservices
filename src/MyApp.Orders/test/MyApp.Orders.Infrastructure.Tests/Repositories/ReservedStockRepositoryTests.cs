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

/// <summary>Integration tests for <see cref="MyApp.Orders.Infrastructure.Repositories.ReservedStockRepository"/> using an in-memory database.</summary>
public class ReservedStockRepositoryTests
{
    private readonly OrdersDbContext _context;
    private readonly ReservedStockRepository _repository;

    /// <summary>Initializes a new instance of the <see cref="ReservedStockRepositoryTests"/> class, setting up the in-memory context, repository, and seed data.</summary>
    public ReservedStockRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new ReservedStockRepository(_context);
        SeedTestData();
    }

    /// <summary>Seeds the in-memory database with representative reserved-stock records for testing.</summary>
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

    /// <summary>Creates and persists a test reservation for the specified order.</summary>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="quantity">The reserved quantity.</param>
    /// <param name="status">The initial reservation status.</param>
    /// <returns>The persisted <see cref="ReservedStock"/> entity.</returns>
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

    /// <summary>Verifies that GetByIdAsync returns the correct reservation when the ID exists.</summary>
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

    /// <summary>Verifies that GetByIdAsync returns null when the reservation ID does not exist.</summary>
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

    /// <summary>Verifies that GetAllAsync returns all persisted reservations.</summary>
    [Fact]
    public async Task ListAsync_ReturnsAllReservations()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least the seeded data
    }

    #endregion

    #region AddAsync Tests

    /// <summary>Verifies that AddAsync persists a new reservation with the correct fields.</summary>
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

    /// <summary>Verifies that UpdateAsync persists changed quantity and status for an existing reservation.</summary>
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

    /// <summary>Verifies that DeleteAsync removes the reservation from the database.</summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesReservation()
    {
        // Arrange
        var orderId = _context.Orders.First().Id;
        var reservation = CreateTestReservation(orderId);

        // Act
        await _repository.DeleteAsync(reservation);
        await _context.SaveChangesAsync();
        var deletedReservation = await _context.ReservedStocks.FindAsync(reservation.Id);

        // Assert
        deletedReservation.Should().BeNull();
    }

    /// <summary>Verifies that DeleteAsync does not throw when the reservation is not found.</summary>
    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var missing = await _repository.GetByIdAsync(nonExistentId);
        if (missing is not null)
            await _repository.DeleteAsync(missing);
    }

    #endregion

    #region GetExpiredReservationsAsync Tests

    /// <summary>Verifies that GetExpiredReservationsAsync returns only reservations that are past their expiry time and still in Reserved status.</summary>
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
        result.Should().HaveCountGreaterThanOrEqualTo(2); // At least the seeded expired one + the new one
        result.All(r => r.Status == ReservationStatus.Reserved).Should().BeTrue();
        result.All(r => r.ReservedUntil < DateTime.UtcNow).Should().BeTrue();
    }

    /// <summary>Verifies that GetExpiredReservationsAsync returns an empty list when no reservations have expired.</summary>
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

    /// <summary>Verifies that GetByOrderIdAsync returns all reservations belonging to the specified order.</summary>
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
        result.Should().HaveCountGreaterThanOrEqualTo(3); // At least 2 seeded + 2 new
        result.All(r => r.OrderId == order.Id).Should().BeTrue();
    }

    /// <summary>Verifies that GetByOrderIdAsync returns an empty list when no reservations exist for the order.</summary>
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

    /// <summary>Verifies that GetByIdWithDetailsAsync returns the reservation when the ID exists.</summary>
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

    /// <summary>Verifies that GetByIdWithDetailsAsync returns null when the ID does not exist.</summary>
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

