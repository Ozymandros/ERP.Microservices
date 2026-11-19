using FluentAssertions;
using Moq;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Internals;
using MyApp.Billing.Application.UseCases.Invoices;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MyApp.Billing.Application.Tests.UseCases;

/// <summary>Tests for <see cref="CreateInvoiceUseCase"/>.</summary>
public class CreateInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidDto_PublishesBillingInvoiceCreated()
    {
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var eventPublisher = new Mock<IEventPublisher>();

        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        invoiceRepo.Setup(r => r.GetByInvoiceNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);
        invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var boundary = new BillingTransactionBoundary(unitOfWork.Object, eventPublisher.Object, NullLogger<BillingTransactionBoundary>.Instance);
        var sut = new CreateInvoiceUseCase(invoiceRepo.Object, boundary);

        var dto = new CreateInvoiceDto(
            "INV-UC-1",
            Guid.NewGuid(),
            null,
            "USD",
            [new CreateInvoiceLineDto("Item", 1, 10m, 0m, 0m)]);

        var result = await sut.ExecuteAsync(dto);

        result.Status.Should().Be("Draft");
        eventPublisher.Verify(e => e.PublishAsync(
            MessagingConstants.Topics.BillingInvoiceCreated,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}