using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Sales.API.EventHandlers;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Sales.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="OrderEventHandlers"/>.</summary>
public class OrderEventHandlersTests
{
    private readonly Mock<ISalesOrderService> _salesOrderService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<OrderEventHandlers>> _logger = new();
    private readonly OrderEventHandlers _sut;

    public OrderEventHandlersTests()
    {
        _sut = new OrderEventHandlers(_salesOrderService.Object, _cacheService.Object, _logger.Object);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_WhenInvalidationReturned_InvalidatesCacheAndReturnsOk()
    {
        var salesOrderId = Guid.NewGuid();
        const string orderNumber = "SO-100";
        var invalidation = new SalesOrderCacheInvalidationDto(salesOrderId, orderNumber);
        var @event = SampleFulfilledEvent();

        _salesOrderService
            .Setup(s => s.MarkShippedFromFulfillmentAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidation);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _salesOrderService.Verify(
            s => s.MarkShippedFromFulfillmentAsync(@event, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-{salesOrderId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-Code-{orderNumber}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("all_sales_orders"), Times.Once);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_WhenNoInvalidation_SkipsCacheAndReturnsOk()
    {
        var @event = SampleFulfilledEvent();
        _salesOrderService
            .Setup(s => s.MarkShippedFromFulfillmentAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrderCacheInvalidationDto?)null);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnOrderCancelledAsync_WhenInvalidationReturned_InvalidatesCacheAndReturnsOk()
    {
        var salesOrderId = Guid.NewGuid();
        const string orderNumber = "SO-200";
        var invalidation = new SalesOrderCacheInvalidationDto(salesOrderId, orderNumber);
        var @event = new OrderCancelledEvent(Guid.NewGuid(), "Customer request", Guid.NewGuid());

        _salesOrderService
            .Setup(s => s.MarkCancelledFromOrderAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidation);

        var result = await _sut.OnOrderCancelledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-{salesOrderId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-Code-{orderNumber}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("all_sales_orders"), Times.Once);
    }

    [Fact]
    public async Task OnOrderCancelledAsync_WhenNoInvalidation_SkipsCacheAndReturnsOk()
    {
        var @event = new OrderCancelledEvent(Guid.NewGuid(), "reason", null);
        _salesOrderService
            .Setup(s => s.MarkCancelledFromOrderAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrderCacheInvalidationDto?)null);

        var result = await _sut.OnOrderCancelledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    private static OrderFulfilledEvent SampleFulfilledEvent() =>
        new(
            Guid.NewGuid(),
            "OP-001",
            "Outbound",
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            [new OrderLineEvent(Guid.NewGuid(), 2)]);
}