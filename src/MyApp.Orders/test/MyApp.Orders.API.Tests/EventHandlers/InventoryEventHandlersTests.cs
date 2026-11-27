using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Orders.API.EventHandlers;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Orders.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="InventoryEventHandlers"/>.</summary>
public class InventoryEventHandlersTests
{
    private readonly Mock<IReservedStockRepository> _reservedStockRepository = new();
    private readonly Mock<ILogger<InventoryEventHandlers>> _logger = new();
    private readonly InventoryEventHandlers _sut;

    public InventoryEventHandlersTests()
    {
        _sut = new InventoryEventHandlers(_reservedStockRepository.Object, _logger.Object);
    }

    [Fact]
    public async Task OnStockReservedAsync_WhenMatchingReservation_ReturnsOk()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var @event = new StockReservedEvent(Guid.NewGuid(), productId, warehouseId, orderId, 5);

        var reservation = new ReservedStock(Guid.NewGuid())
        {
            OrderId = orderId,
            ProductId = productId,
            WarehouseId = warehouseId
        };

        _reservedStockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync([reservation]);

        var result = await _sut.OnStockReservedAsync(@event);

        result.Should().BeOfType<OkResult>();
        _reservedStockRepository.Verify(r => r.GetByOrderIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task OnStockReservedAsync_WhenNoMatchingReservation_ReturnsOk()
    {
        var orderId = Guid.NewGuid();
        var @event = new StockReservedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), orderId, 1);

        _reservedStockRepository
            .Setup(r => r.GetByOrderIdAsync(orderId))
            .ReturnsAsync([]);

        var result = await _sut.OnStockReservedAsync(@event);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task OnStockReleasedAsync_WhenReservationExists_ReturnsOk()
    {
        var reservationId = Guid.NewGuid();
        var @event = new StockReleasedEvent(reservationId, Guid.NewGuid(), Guid.NewGuid(), 3);

        _reservedStockRepository
            .Setup(r => r.GetByIdAsync(reservationId))
            .ReturnsAsync(new ReservedStock(reservationId));

        var result = await _sut.OnStockReleasedAsync(@event);

        result.Should().BeOfType<OkResult>();
        _reservedStockRepository.Verify(r => r.GetByIdAsync(reservationId), Times.Once);
    }

    [Fact]
    public void OnLowStockAlertAsync_ReturnsOk()
    {
        var @event = new LowStockAlertEvent(Guid.NewGuid(), Guid.NewGuid(), 1, 20);

        var result = _sut.OnLowStockAlertAsync(@event);

        result.Should().BeOfType<OkResult>();
    }
}