using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Sales.API.EventHandlers;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Sales.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="BillingEventHandlers"/>.</summary>
public class BillingEventHandlersTests
{
    private readonly Mock<ISalesOrderService> _salesOrderService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly BillingEventHandlers _sut;

    public BillingEventHandlersTests()
    {
        _sut = new BillingEventHandlers(_salesOrderService.Object, _cacheService.Object);
    }

    [Fact]
    public async Task OnInvoiceIssuedAsync_WhenInvalidationReturned_InvalidatesCacheAndReturnsOk()
    {
        var salesOrderId = Guid.NewGuid();
        const string orderNumber = "SO-INV";
        var invalidation = new SalesOrderCacheInvalidationDto(salesOrderId, orderNumber);
        var @event = new InvoiceIssuedEvent(
            Guid.NewGuid(),
            "INV-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            20m,
            120m,
            DateTime.UtcNow.AddDays(30));

        _salesOrderService
            .Setup(s => s.MarkInvoicedFromBillingAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidation);

        var result = await _sut.OnInvoiceIssuedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _salesOrderService.Verify(
            s => s.MarkInvoicedFromBillingAsync(@event, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-{salesOrderId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"SalesOrder-Code-{orderNumber}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("all_sales_orders"), Times.Once);
    }

    [Fact]
    public async Task OnInvoiceIssuedAsync_WhenNoInvalidation_SkipsCacheAndReturnsOk()
    {
        var @event = new InvoiceIssuedEvent(
            Guid.NewGuid(),
            "INV-2",
            Guid.NewGuid(),
            null,
            50m,
            10m,
            60m,
            DateTime.UtcNow);

        _salesOrderService
            .Setup(s => s.MarkInvoicedFromBillingAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrderCacheInvalidationDto?)null);

        var result = await _sut.OnInvoiceIssuedAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }
}