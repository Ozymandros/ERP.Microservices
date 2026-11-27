using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.API.EventHandlers;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Inventory.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="OrderEventHandlers"/>.</summary>
public class OrderEventHandlersTests
{
    private readonly Mock<IWarehouseStockRepository> _warehouseStockRepository = new();
    private readonly Mock<IInventoryTransactionRepository> _transactionRepository = new();
    private readonly Mock<ILogger<OrderEventHandlers>> _logger = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly OrderEventHandlers _sut;

    public OrderEventHandlersTests()
    {
        _sut = new OrderEventHandlers(
            _warehouseStockRepository.Object,
            _transactionRepository.Object,
            _logger.Object,
            _eventPublisher.Object);
    }

    [Fact]
    public async Task OnOrderCreatedAsync_NonInbound_ReturnsOkWithoutRepositoryCalls()
    {
        var @event = new OrderCreatedEvent(
            Guid.NewGuid(),
            "OP-1",
            OrderTypes.Outbound,
            Guid.NewGuid(),
            [new OrderLineEvent(Guid.NewGuid(), 1)]);

        var result = await _sut.OnOrderCreatedAsync(@event);

        result.Should().BeOfType<OkResult>();
        _warehouseStockRepository.Verify(
            r => r.GetByProductAndWarehouseAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnOrderCreatedAsync_Inbound_UpdatesOnOrderQuantityAndReturnsOk()
    {
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var @event = new OrderCreatedEvent(
            Guid.NewGuid(),
            "OP-IN",
            OrderTypes.Inbound,
            warehouseId,
            [new OrderLineEvent(productId, 4)]);

        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            OnOrderQuantity = 1
        };

        _warehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        var result = await _sut.OnOrderCreatedAsync(@event);

        result.Should().BeOfType<OkResult>();
        _warehouseStockRepository.Verify(r => r.UpdateAsync(stock), Times.Once);
        stock.OnOrderQuantity.Should().Be(5);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_Inbound_UpdatesStockPublishesEventAndReturnsOk()
    {
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var @event = new OrderFulfilledEvent(
            Guid.NewGuid(),
            "OP-FIN",
            OrderTypes.Inbound,
            warehouseId,
            DateTime.UtcNow,
            null,
            null,
            [new OrderLineEvent(productId, 3)]);

        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 10,
            OnOrderQuantity = 3
        };

        _warehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId, default))
            .ReturnsAsync(stock);

        var result = await _sut.OnOrderFulfilledAsync(@event);

        result.Should().BeOfType<OkResult>();
        _warehouseStockRepository.Verify(r => r.UpdateAsync(stock), Times.Once);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<InventoryTransaction>()), Times.Once);
        _eventPublisher.Verify(
            p => p.PublishAsync(
                MessagingConstants.Topics.InventoryStockUpdated,
                It.IsAny<StockUpdatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        stock.AvailableQuantity.Should().Be(13);
        stock.OnOrderQuantity.Should().Be(0);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_Transfer_ReturnsOkWithoutStockUpdate()
    {
        var warehouseId = Guid.NewGuid();
        var @event = new OrderFulfilledEvent(
            Guid.NewGuid(),
            "OP-TR",
            OrderTypes.Transfer,
            warehouseId,
            DateTime.UtcNow,
            null,
            null,
            [new OrderLineEvent(Guid.NewGuid(), 2)]);

        var result = await _sut.OnOrderFulfilledAsync(@event);

        result.Should().BeOfType<OkResult>();
        _warehouseStockRepository.Verify(r => r.UpdateAsync(It.IsAny<WarehouseStock>()), Times.Never);
        _eventPublisher.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}