using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Billing.API.EventHandlers;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using Xunit;

namespace MyApp.Billing.API.Tests.EventHandlers;

/// <summary>Unit tests for <see cref="OrderEventHandlers"/>.</summary>
public class OrderEventHandlersTests
{
    private readonly Mock<IInvoiceService> _invoiceService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<OrderEventHandlers>> _logger = new();
    private readonly OrderEventHandlers _sut;

    public OrderEventHandlersTests()
    {
        _sut = new OrderEventHandlers(_invoiceService.Object, _cacheService.Object, _logger.Object);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_Outbound_WithInvoice_InvalidatesInvoiceCacheAndReturnsOk()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = SampleInvoice(orderId, customerId, "INV-OUT");
        var @event = SampleFulfilledEvent(OrderTypes.Outbound);

        _invoiceService
            .Setup(s => s.CreateAndIssueFromFulfillmentAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _invoiceService.Verify(
            s => s.CreateAndIssueFromFulfillmentAsync(@event, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoice_{invoice.Id}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoices_customer_{customerId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoices_order_{orderId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"Invoice-Number-{invoice.InvoiceNumber}"), Times.Once);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_Return_WithCreditNote_InvalidatesOpenInvoicesAndReturnsOk()
    {
        var @event = SampleFulfilledEvent(OrderTypes.Return);
        _invoiceService
            .Setup(s => s.CreateCreditNoteFromReturnFulfillmentAsync(@event, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditNoteDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Return",
                "Issued",
                50m,
                10m,
                60m,
                DateTime.UtcNow));

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
        _invoiceService.Verify(
            s => s.CreateAndIssueFromFulfillmentAsync(It.IsAny<OrderFulfilledEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnOrderFulfilledAsync_Inbound_DoesNotCreateInvoice_ReturnsOk()
    {
        var @event = SampleFulfilledEvent(OrderTypes.Inbound);

        var result = await _sut.OnOrderFulfilledAsync(@event, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _invoiceService.Verify(
            s => s.CreateAndIssueFromFulfillmentAsync(It.IsAny<OrderFulfilledEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheService.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    private static OrderFulfilledEvent SampleFulfilledEvent(string orderType) =>
        new(
            Guid.NewGuid(),
            "OP-100",
            orderType,
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            [new OrderLineEvent(Guid.NewGuid(), 1)]);

    private static InvoiceDto SampleInvoice(Guid orderId, Guid customerId, string invoiceNumber) =>
        new(
            Guid.NewGuid(),
            invoiceNumber,
            customerId,
            orderId,
            "EUR",
            "Issued",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            100m,
            20m,
            120m,
            120m,
            [],
            DateTime.UtcNow,
            DateTime.UtcNow);
}