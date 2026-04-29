using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Billing.Application.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo;
    private readonly Mock<ICreditNoteRepository> _creditNoteRepo;
    private readonly Mock<ILogger<InvoiceService>> _logger;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        _invoiceRepo = new Mock<IInvoiceRepository>();
        _creditNoteRepo = new Mock<ICreditNoteRepository>();
        _logger = new Mock<ILogger<InvoiceService>>();
        _eventPublisher = new Mock<IEventPublisher>();

        _sut = new InvoiceService(
            _invoiceRepo.Object,
            _creditNoteRepo.Object,
            _logger.Object,
            _eventPublisher.Object);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static Invoice BuildDraftInvoice(Guid? customerId = null)
    {
        var inv = new Invoice(Guid.NewGuid(), customerId ?? Guid.NewGuid(), "USD");
        inv.AddLine("Widget", 2, 50m, 10m);
        return inv;
    }

    private static Invoice BuildIssuedInvoice()
    {
        var inv = BuildDraftInvoice();
        inv.Issue("INV-001", DateTime.UtcNow, 30);
        return inv;
    }

    private static CreateInvoiceDto SampleCreateDto(int lineCount = 1) =>
        new(
            CustomerId: Guid.NewGuid(),
            OrderId: null,
            Currency: "USD",
            Lines: Enumerable.Range(1, lineCount)
                .Select(i => new CreateInvoiceLineDto($"Item {i}", i, 100m, 10m, 0m))
                .ToList(),
            PaymentTermsDays: 30);

    // ─── CreateInvoiceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoiceAsync_ValidDto_AddsInvoiceAndReturnsDto()
    {
        // Arrange
        var dto = SampleCreateDto(2);
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateInvoiceAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(dto.CustomerId);
        result.Currency.Should().Be("USD");
        result.Status.Should().Be("Draft");
        result.Lines.Should().HaveCount(2);

        _invoiceRepo.Verify(r => r.AddAsync(It.Is<Invoice>(i =>
            i.CustomerId == dto.CustomerId &&
            i.Currency == "USD" &&
            i.Lines.Count == 2 &&
            i.Status == InvoiceStatus.Draft)), Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PublishesInvoiceCreatedEvent()
    {
        // Arrange
        var dto = SampleCreateDto();
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateInvoiceAsync(dto);

        // Assert
        _eventPublisher.Verify(e => e.PublishAsync(
            "billing.invoice.created",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithOrderId_SetsOrderIdOnReturnedDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dto = new CreateInvoiceDto(Guid.NewGuid(), orderId, "EUR",
            new List<CreateInvoiceLineDto> { new("Product A", 1, 200m, 20m, 0m) }, 14);
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateInvoiceAsync(dto);

        // Assert
        result.Currency.Should().Be("EUR");
    }

    // ─── IssueInvoiceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task IssueInvoiceAsync_ExistingDraftInvoice_ReturnsIssuedDto()
    {
        // Arrange
        var invoice = BuildDraftInvoice();
        var issueDate = DateTime.UtcNow;
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.IssueInvoiceAsync(invoice.Id, "INV-100", issueDate);

        // Assert
        result.Status.Should().Be("Issued");
        result.InvoiceNumber.Should().Be("INV-100");
        result.IssueDate.Should().BeCloseTo(issueDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task IssueInvoiceAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Invoice?)null);

        // Act
        Func<Task> act = () => _sut.IssueInvoiceAsync(id, "INV-999", DateTime.UtcNow);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task IssueInvoiceAsync_PublishesInvoiceIssuedEvent()
    {
        // Arrange
        var invoice = BuildDraftInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.IssueInvoiceAsync(invoice.Id, "INV-200", DateTime.UtcNow);

        // Assert
        _eventPublisher.Verify(e => e.PublishAsync(
            "billing.invoice.issued",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueInvoiceAsync_UpdatesRepositoryWithIssuedInvoice()
    {
        // Arrange
        var invoice = BuildDraftInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.IssueInvoiceAsync(invoice.Id, "INV-300", DateTime.UtcNow);

        // Assert
        _invoiceRepo.Verify(r => r.UpdateAsync(It.Is<Invoice>(i =>
            i.Status == InvoiceStatus.Issued &&
            i.InvoiceNumber == "INV-300")), Times.Once);
    }

    // ─── RecordPaymentAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RecordPaymentAsync_ValidPayment_ReturnsUpdatedDto()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var dto = new RecordPaymentDto(invoice.Id, invoice.TotalGross, "BankTransfer", DateTime.UtcNow);
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RecordPaymentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OutstandingAmount.Should().Be(0m);
        result.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task RecordPaymentAsync_PartialPayment_ReducesOutstandingAmount()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var partialAmount = invoice.TotalGross / 2;
        var dto = new RecordPaymentDto(invoice.Id, partialAmount, "Card", DateTime.UtcNow);
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RecordPaymentAsync(dto);

        // Assert
        result.OutstandingAmount.Should().Be(invoice.TotalGross - partialAmount);
        result.Status.Should().Be("Issued");
    }

    [Fact]
    public async Task RecordPaymentAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RecordPaymentDto(id, 100m, "Cash", DateTime.UtcNow);
        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Invoice?)null);

        // Act
        Func<Task> act = () => _sut.RecordPaymentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task RecordPaymentAsync_PublishesInvoicePaidEvent()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var dto = new RecordPaymentDto(invoice.Id, invoice.TotalGross, "BankTransfer", DateTime.UtcNow);
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordPaymentAsync(dto);

        // Assert
        _eventPublisher.Verify(e => e.PublishAsync(
            "billing.invoice.paid",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── CancelInvoiceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CancelInvoiceAsync_IssuedInvoice_ReturnsCancelledDto()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CancelInvoiceAsync(invoice.Id, "Customer request");

        // Assert
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelInvoiceAsync_DraftInvoice_ReturnsCancelledDto()
    {
        // Arrange
        var invoice = BuildDraftInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CancelInvoiceAsync(invoice.Id, "Duplicate");

        // Assert
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelInvoiceAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Invoice?)null);

        // Act
        Func<Task> act = () => _sut.CancelInvoiceAsync(id, "reason");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelInvoiceAsync_PublishesInvoiceCancelledEvent()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.CancelInvoiceAsync(invoice.Id, "Test reason");

        // Assert
        _eventPublisher.Verify(e => e.PublishAsync(
            "billing.invoice.cancelled",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelInvoiceAsync_PaidInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        invoice.RecordPayment(invoice.TotalGross, "Card", DateTime.UtcNow);   // now Paid
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        // Act
        Func<Task> act = () => _sut.CancelInvoiceAsync(invoice.Id, "mistake");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel a paid invoice*");
    }

    // ─── CreateCreditNoteAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateCreditNoteAsync_IssuedInvoice_ReturnsCreditNoteDto()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var dto = new CreateCreditNoteDto(
            invoice.Id,
            new List<CreditNoteLineDto> { new("Refund", 1, 50m, 10m, 0m) },
            "Returned goods");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _creditNoteRepo.Setup(r => r.AddAsync(It.IsAny<CreditNote>())).Returns<CreditNote>(cn => Task.FromResult(cn));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateCreditNoteAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OriginalInvoiceId.Should().Be(invoice.Id);
        result.Reason.Should().Be("Returned goods");
        result.Status.Should().Be("Issued");
        result.TotalGross.Should().BePositive();
    }

    [Fact]
    public async Task CreateCreditNoteAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateCreditNoteDto(id,
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) },
            "reason");
        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Invoice?)null);

        // Act
        Func<Task> act = () => _sut.CreateCreditNoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateCreditNoteAsync_DraftInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = BuildDraftInvoice();   // still Draft – credit note not allowed
        var dto = new CreateCreditNoteDto(invoice.Id,
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) },
            "reason");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        // Act
        Func<Task> act = () => _sut.CreateCreditNoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    [Fact]
    public async Task CreateCreditNoteAsync_AddsCreditNoteToRepository()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var dto = new CreateCreditNoteDto(invoice.Id,
            new List<CreditNoteLineDto> { new("Partial refund", 1, 30m, 10m, 0m) },
            "Damage");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _creditNoteRepo.Setup(r => r.AddAsync(It.IsAny<CreditNote>())).Returns<CreditNote>(cn => Task.FromResult(cn));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateCreditNoteAsync(dto);

        // Assert
        _creditNoteRepo.Verify(r => r.AddAsync(It.Is<CreditNote>(cn =>
            cn.OriginalInvoiceId == invoice.Id &&
            cn.Reason == "Damage")), Times.Once);
    }

    [Fact]
    public async Task CreateCreditNoteAsync_PublishesCreditNoteIssuedEvent()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        var dto = new CreateCreditNoteDto(invoice.Id,
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) },
            "Testing");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _creditNoteRepo.Setup(r => r.AddAsync(It.IsAny<CreditNote>())).Returns<CreditNote>(cn => Task.FromResult(cn));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateCreditNoteAsync(dto);

        // Assert
        _eventPublisher.Verify(e => e.PublishAsync(
            "billing.creditnote.issued",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── GetInvoiceByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        // Act
        var result = await _sut.GetInvoiceByIdAsync(invoice.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(invoice.Id);
        result.Status.Should().Be("Issued");
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Invoice?)null);

        // Act
        var result = await _sut.GetInvoiceByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    // ─── GetInvoicesByCustomerIdAsync ────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesByCustomerIdAsync_ReturnsAllCustomerInvoices()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var invoices = new List<Invoice>
        {
            new Invoice(Guid.NewGuid(), customerId, "USD"),
            new Invoice(Guid.NewGuid(), customerId, "EUR"),
        };
        _invoiceRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetInvoicesByCustomerIdAsync(customerId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetInvoicesByCustomerIdAsync_NoInvoices_ReturnsEmptyList()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Invoice>());

        // Act
        var result = await _sut.GetInvoicesByCustomerIdAsync(customerId);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── GetOpenInvoicesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetOpenInvoicesAsync_ReturnsMappedDtos()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            BuildIssuedInvoice(),
            BuildIssuedInvoice(),
            BuildIssuedInvoice(),
        };
        _invoiceRepo.Setup(r => r.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetOpenInvoicesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(dto => dto.Status.Should().Be("Issued"));
    }

    [Fact]
    public async Task GetOpenInvoicesAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _invoiceRepo.Setup(r => r.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Invoice>());

        // Act
        var result = await _sut.GetOpenInvoicesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ─── GetInvoicesByOrderIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesByOrderIdAsync_ReturnsAllOrderInvoices()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var invoices = new List<Invoice> { BuildIssuedInvoice() };
        _invoiceRepo.Setup(r => r.GetInvoicesByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetInvoicesByOrderIdAsync(orderId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInvoicesByOrderIdAsync_NoInvoices_ReturnsEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetInvoicesByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Invoice>());

        // Act
        var result = await _sut.GetInvoicesByOrderIdAsync(orderId);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── DTO mapping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoiceAsync_MapsLinesTotalCorrectly()
    {
        // Arrange – 1 line: qty 2, price 100, tax 10%, discount 0
        // LineNet = 200, LineTax = 20, LineGross = 220
        var dto = new CreateInvoiceDto(Guid.NewGuid(), null, "USD",
            new List<CreateInvoiceLineDto> { new("Widget", 2, 100m, 10m, 0m) }, 30);
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns<Invoice>(i => Task.FromResult(i));
        _eventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateInvoiceAsync(dto);

        // Assert
        result.TotalNet.Should().Be(200m);
        result.TotalTax.Should().Be(20m);
        result.TotalGross.Should().Be(220m);
        result.OutstandingAmount.Should().Be(220m);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_MapsAllFieldsCorrectly()
    {
        // Arrange
        var invoice = BuildIssuedInvoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        // Act
        var result = await _sut.GetInvoiceByIdAsync(invoice.Id);

        // Assert
        result!.Id.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("INV-001");
        result.CustomerId.Should().Be(invoice.CustomerId);
        result.Currency.Should().Be("USD");
        result.TotalNet.Should().Be(invoice.TotalNet);
        result.TotalGross.Should().Be(invoice.TotalGross);
        result.IssueDate.Should().NotBeNull();
        result.DueDate.Should().NotBeNull();
        result.Lines.Should().HaveCount(1);
    }
}
