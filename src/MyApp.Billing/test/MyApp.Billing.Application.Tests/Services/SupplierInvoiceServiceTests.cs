using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Billing.Application.Services;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.UseCases.SupplierInvoices;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Billing.Application.Tests.Services;

public class SupplierInvoiceServiceTests
{
    private readonly Mock<ISupplierInvoiceRepository> _repo = new();
    private readonly SupplierInvoiceService _sut;

    public SupplierInvoiceServiceTests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        var boundary = new BillingTransactionBoundary(
            unitOfWork.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<BillingTransactionBoundary>.Instance);
        var create = new CreateSupplierInvoiceUseCase(_repo.Object, boundary);
        _sut = new SupplierInvoiceService(_repo.Object, create);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierInvoice?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_MapsLines()
    {
        var id = Guid.NewGuid();
        var entity = new SupplierInvoice(id)
        {
            InvoiceNumber = "SINV-9",
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            Currency = "EUR",
            Lines =
            [
                new SupplierInvoiceLine(Guid.NewGuid())
                {
                    SupplierInvoiceId = id,
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 9.5m
                }
            ]
        };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be("SINV-9");
        result.Lines.Should().ContainSingle(l => l.UnitPrice == 9.5m);
    }

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_ReturnsMappedList()
    {
        var poId = Guid.NewGuid();
        var entity = new SupplierInvoice(Guid.NewGuid())
        {
            InvoiceNumber = "SINV-PO",
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = poId
        };
        _repo.Setup(r => r.GetByPurchaseOrderIdAsync(poId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([entity]);

        var result = await _sut.GetByPurchaseOrderIdAsync(poId);

        result.Should().ContainSingle(d => d.PurchaseOrderId == poId);
    }
}