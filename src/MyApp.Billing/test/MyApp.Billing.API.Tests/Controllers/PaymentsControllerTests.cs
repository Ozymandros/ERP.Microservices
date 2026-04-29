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

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<PaymentsController>> _logger;
    private readonly PaymentsController _sut;

    public PaymentsControllerTests()
    {
        _paymentRepo = new Mock<IPaymentRepository>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<PaymentsController>>();

        _sut = new PaymentsController(
            _paymentRepo.Object,
            _cacheService.Object,
            _logger.Object);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static Payment BuildPayment(Guid invoiceId) =>
        new(Guid.NewGuid(), invoiceId, 110m, "USD", "Card", DateTime.UtcNow);

    private static PaymentDto BuildPaymentDto(Guid invoiceId) =>
        new(Guid.NewGuid(), invoiceId, 110m, "USD", "Card", "Completed", DateTime.UtcNow);

    // ─── GetByInvoice ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoice_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var cached = new List<PaymentDto> { BuildPaymentDto(invoiceId) };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cached);
        _paymentRepo.Verify(r => r.GetByInvoiceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByInvoice_CacheMiss_QueriesRepository()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var payments = new List<Payment> { BuildPayment(invoiceId) };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<PaymentDto>?)null);
        _paymentRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _paymentRepo.Verify(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByInvoice_CacheMiss_MapsPaymentsToDto()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var payments = new List<Payment>
        {
            BuildPayment(invoiceId),
            BuildPayment(invoiceId)
        };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<PaymentDto>?)null);
        _paymentRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<PaymentDto>>().Subject;
        dtos.Should().HaveCount(2);
        dtos.Should().AllSatisfy(d => d.InvoiceId.Should().Be(invoiceId));
    }

    [Fact]
    public async Task GetByInvoice_NoPayments_ReturnsEmptyList()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<PaymentDto>?)null);
        _paymentRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Payment>());

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<PaymentDto>>().Subject;
        dtos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInvoice_MapsMethodAndStatusCorrectly()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var payment = new Payment(Guid.NewGuid(), invoiceId, 55m, "EUR", "BankTransfer", DateTime.UtcNow);
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<PaymentDto>?)null);
        _paymentRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Payment> { payment });

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeAssignableTo<IEnumerable<PaymentDto>>().Subject.Single();
        dto.Method.Should().Be("BankTransfer");
        dto.Status.Should().Be("Completed");
        dto.Currency.Should().Be("EUR");
        dto.Amount.Should().Be(55m);
    }

    [Fact]
    public async Task GetByInvoice_RepositoryThrows_Returns500()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PaymentDto>>($"payments_invoice_{invoiceId}"))
                     .ReturnsAsync((IEnumerable<PaymentDto>?)null);
        _paymentRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Connection lost"));

        // Act
        var result = await _sut.GetByInvoice(invoiceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
