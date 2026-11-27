using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Sales.API.EventHandlers;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Sales.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="InventoryEventHandlers"/>.</summary>
public class InventoryEventHandlersTests
{
    private readonly Mock<ILogger<InventoryEventHandlers>> _logger = new();
    private readonly InventoryEventHandlers _sut;

    public InventoryEventHandlersTests()
    {
        _sut = new InventoryEventHandlers(_logger.Object);
    }

    [Fact]
    public void OnLowStockAlertAsync_ReturnsOk()
    {
        var @event = new LowStockAlertEvent(Guid.NewGuid(), Guid.NewGuid(), 2, 10);

        var result = _sut.OnLowStockAlertAsync(@event);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public void OnStockUpdatedAsync_ReturnsOk()
    {
        var @event = new StockUpdatedEvent(Guid.NewGuid(), Guid.NewGuid(), -5, "Outbound");

        var result = _sut.OnStockUpdatedAsync(@event);

        result.Should().BeOfType<OkResult>();
    }
}