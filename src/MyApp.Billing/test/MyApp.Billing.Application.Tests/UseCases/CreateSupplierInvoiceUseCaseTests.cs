using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.UseCases.SupplierInvoices;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Billing.Application.Tests.UseCases;

public class CreateSupplierInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidDto_PersistsAndReturnsMappedDto()
    {
        var repo = new Mock<ISupplierInvoiceRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var eventPublisher = new Mock<IEventPublisher>();
        SupplierInvoice? saved = null;

        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        repo.Setup(r => r.AddAsync(It.IsAny<SupplierInvoice>()))
            .Returns<SupplierInvoice>(entity =>
            {
                saved = entity;
                return Task.FromResult(entity);
            });

        var boundary = new BillingTransactionBoundary(
            unitOfWork.Object,
            eventPublisher.Object,
            NullLogger<BillingTransactionBoundary>.Instance);
        var sut = new CreateSupplierInvoiceUseCase(repo.Object, boundary);

        var productId = Guid.NewGuid();
        var dto = new CreateSupplierInvoiceDto(
            "SI-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EUR",
            [new CreateSupplierInvoiceLineDto(productId, 2, 25m)]);

        var result = await sut.ExecuteAsync(dto);

        result.InvoiceNumber.Should().Be("SI-1");
        result.Currency.Should().Be("EUR");
        result.Lines.Should().ContainSingle(l => l.ProductId == productId && l.Quantity == 2 && l.UnitPrice == 25m);
        saved.Should().NotBeNull();
        saved!.Lines.Should().AllSatisfy(l => l.SupplierInvoiceId.Should().Be(saved.Id));
        unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
