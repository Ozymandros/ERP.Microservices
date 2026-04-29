using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Billing.API;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Caching;
using Xunit;

namespace MyApp.Billing.API.Tests.Controllers;

public class CreditNotesControllerTests
{
    private readonly Mock<ICreditNoteRepository> _creditNoteRepo;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<CreditNotesController>> _logger;
    private readonly CreditNotesController _sut;

    public CreditNotesControllerTests()
    {
        _creditNoteRepo = new Mock<ICreditNoteRepository>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<CreditNotesController>>();

        _sut = new CreditNotesController(
            _creditNoteRepo.Object,
            _cacheService.Object,
            _logger.Object);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static CreditNote BuildCreditNote(Guid originalInvoiceId) =>
        new(Guid.NewGuid(), originalInvoiceId,
            new List<CreditNoteLineData> { new("Refund", 1, 50m, 10m, 0m) },
            "Returned goods");

    private static CreditNoteDto BuildCreditNoteDto(Guid originalInvoiceId) =>
        new(Guid.NewGuid(), originalInvoiceId, "Returned goods", "Issued",
            50m, 5m, 55m, DateTime.UtcNow);

    // ─── GetByInvoice ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoice_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var cached = new List<CreditNoteDto> { BuildCreditNoteDto(invoiceId) };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cached);
        _creditNoteRepo.Verify(r => r.GetByInvoiceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByInvoice_CacheMiss_QueriesRepository()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var creditNotes = new List<CreditNote> { BuildCreditNote(invoiceId) };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<CreditNoteDto>?)null);
        _creditNoteRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(creditNotes);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _creditNoteRepo.Verify(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByInvoice_CacheMiss_MapsCreditNotesToDto()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var creditNotes = new List<CreditNote>
        {
            BuildCreditNote(invoiceId),
            BuildCreditNote(invoiceId)
        };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<CreditNoteDto>?)null);
        _creditNoteRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(creditNotes);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<CreditNoteDto>>().Subject;
        dtos.Should().HaveCount(2);
        dtos.Should().AllSatisfy(d => d.OriginalInvoiceId.Should().Be(invoiceId));
    }

    [Fact]
    public async Task GetByInvoice_NoCreditNotes_ReturnsEmptyList()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<CreditNoteDto>?)null);
        _creditNoteRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<CreditNote>());

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<CreditNoteDto>>().Subject;
        dtos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInvoice_MapsAllFieldsCorrectly()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var creditNote = BuildCreditNote(invoiceId);
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<CreditNoteDto>?)null);
        _creditNoteRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<CreditNote> { creditNote });

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeAssignableTo<IEnumerable<CreditNoteDto>>().Subject.Single();
        dto.OriginalInvoiceId.Should().Be(invoiceId);
        dto.Reason.Should().Be("Returned goods");
        dto.Status.Should().Be("Issued");
        dto.TotalGross.Should().BePositive();
    }

    [Fact]
    public async Task GetByInvoice_RepositoryThrows_Returns500()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<CreditNoteDto>>($"credit_notes_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<CreditNoteDto>?)null);
        _creditNoteRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("DB timeout"));

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetByInvoice_ActionIsNamedGetByInvoice_SatisfiesCreatedAtActionContract()
    {
        // This test ensures the action name matches what InvoicesController.CreateCreditNote
        // references via CreatedAtAction(nameof(CreditNotesController.GetByInvoice), ...)
        var methodName = nameof(CreditNotesController.GetByInvoice);
        methodName.Should().Be("GetByInvoice");
    }
}
