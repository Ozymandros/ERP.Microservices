using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Purchasing.Application.Internals;
using MyApp.Purchasing.Application.UseCases.PurchaseOrders;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.UseCases;

public class MatchPurchaseOrderUseCaseTests
{
    private readonly Mock<IPurchaseOrderRepository> _orders = new();
    private readonly Mock<ISupplierInvoiceQueryPort> _invoices = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly MatchPurchaseOrderUseCase _sut;

    public MatchPurchaseOrderUseCaseTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _events.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var boundary = new PurchasingTransactionBoundary(
            _uow.Object,
            _events.Object,
            NullLogger<PurchasingTransactionBoundary>.Instance);
        _sut = new MatchPurchaseOrderUseCase(
            _orders.Object,
            _invoices.Object,
            boundary,
            NullLogger<MatchPurchaseOrderUseCase>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_OrderNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _orders.Setup(r => r.GetWithLinesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var act = async () => await _sut.ExecuteAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task ExecuteAsync_NoSupplierInvoices_SetsUnmatched()
    {
        var id = Guid.NewGuid();
        var order = new PurchaseOrder(id)
        {
            OrderNumber = "PO-1",
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = id,
                    ProductId = Guid.NewGuid(),
                    Quantity = 5,
                    ReceivedQuantity = 5
                }
            ]
        };
        _orders.Setup(r => r.GetWithLinesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoices.Setup(i => i.GetByPurchaseOrderIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.ExecuteAsync(id);

        result.MatchStatus.Should().Be(PurchaseOrderMatchStatus.Unmatched);
        order.MatchStatus.Should().Be(PurchaseOrderMatchStatus.Unmatched);
        order.MatchedAt.Should().NotBeNull();
        _events.Verify(e => e.PublishAsync(
            MessagingConstants.Topics.PurchasingOrderMatched,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FullyAligned_SetsMatchedAndPublishes()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = new PurchaseOrder(id)
        {
            OrderNumber = "PO-2",
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = id,
                    ProductId = productId,
                    Quantity = 3,
                    ReceivedQuantity = 3
                }
            ]
        };
        _orders.Setup(r => r.GetWithLinesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoices.Setup(i => i.GetByPurchaseOrderIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SupplierInvoiceSnapshot(
                    Guid.NewGuid(),
                    [new SupplierInvoiceLineSnapshot(productId, 3)])
            ]);

        var result = await _sut.ExecuteAsync(id);

        result.MatchStatus.Should().Be(PurchaseOrderMatchStatus.Matched);
        _events.Verify(e => e.PublishAsync(
            MessagingConstants.Topics.PurchasingOrderMatched,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BilledMoreThanReceived_SetsOverbilled()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = new PurchaseOrder(id)
        {
            OrderNumber = "PO-3",
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = id,
                    ProductId = productId,
                    Quantity = 5,
                    ReceivedQuantity = 2
                }
            ]
        };
        _orders.Setup(r => r.GetWithLinesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoices.Setup(i => i.GetByPurchaseOrderIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SupplierInvoiceSnapshot(
                    Guid.NewGuid(),
                    [new SupplierInvoiceLineSnapshot(productId, 4)])
            ]);

        var result = await _sut.ExecuteAsync(id);

        result.MatchStatus.Should().Be(PurchaseOrderMatchStatus.Overbilled);
        _events.Verify(e => e.PublishAsync(
            MessagingConstants.Topics.PurchasingOrderMatched,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MissingBilledLine_SetsPartial()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var order = new PurchaseOrder(id)
        {
            OrderNumber = "PO-4",
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = id,
                    ProductId = productId,
                    Quantity = 5,
                    ReceivedQuantity = 5
                }
            ]
        };
        _orders.Setup(r => r.GetWithLinesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoices.Setup(i => i.GetByPurchaseOrderIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SupplierInvoiceSnapshot(
                    Guid.NewGuid(),
                    [new SupplierInvoiceLineSnapshot(otherProductId, 5)])
            ]);

        var result = await _sut.ExecuteAsync(id);

        result.MatchStatus.Should().Be(PurchaseOrderMatchStatus.Partial);
    }
}
