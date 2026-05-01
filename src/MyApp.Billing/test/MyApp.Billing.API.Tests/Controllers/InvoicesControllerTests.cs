using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Billing.API.Controllers;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Billing.API.Tests.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _invoiceService;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<InvoicesController>> _logger;
    private readonly InvoicesController _sut;

    public InvoicesControllerTests()
    {
        _invoiceService = new Mock<IInvoiceService>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<InvoicesController>>();

        _sut = new InvoicesController(
            _invoiceService.Object,
            _cacheService.Object,
            _logger.Object);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static InvoiceDto SampleInvoiceDto(string status = "Issued") =>
        new(Guid.NewGuid(), "INV-001", Guid.NewGuid(), null, "USD", status,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            100m, 10m, 110m, 110m,
            new List<InvoiceLineDto>(), DateTime.UtcNow, DateTime.UtcNow);

    private static List<InvoiceDto> SampleInvoiceList(int count = 2) =>
        Enumerable.Range(1, count).Select(_ => SampleInvoiceDto()).ToList();

    private static CreditNoteDto SampleCreditNoteDto() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Returned goods", "Issued",
            90m, 9m, 99m, DateTime.UtcNow);

    // ─── GetOpenInvoices ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetOpenInvoices_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var cached = SampleInvoiceList();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetOpenInvoices(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cached);
        _invoiceService.Verify(s => s.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOpenInvoices_CacheMiss_CallsServiceAndReturns200()
    {
        // Arrange
        var invoices = SampleInvoiceList(3);
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices"))
                     .ReturnsAsync((IEnumerable<InvoiceDto>?)null);
        _invoiceService.Setup(s => s.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetOpenInvoices(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(invoices);
        _invoiceService.Verify(s => s.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOpenInvoices_ServiceThrows_Returns500()
    {
        // Arrange
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices"))
                     .ReturnsAsync((IEnumerable<InvoiceDto>?)null);
        _invoiceService.Setup(s => s.GetOpenInvoicesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await _sut.GetOpenInvoices(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheHit_ReturnsCachedInvoice()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cached = SampleInvoiceDto();
        _cacheService.Setup(c => c.GetStateAsync<InvoiceDto>($"invoice_{id}"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cached);
        _invoiceService.Verify(s => s.GetInvoiceByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_CacheMiss_InvoiceExists_Returns200()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = SampleInvoiceDto();
        _cacheService.Setup(c => c.GetStateAsync<InvoiceDto>($"invoice_{id}"))
                     .ReturnsAsync((InvoiceDto?)null);
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(dto);

        // Act
        var result = await _sut.GetById(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<InvoiceDto>($"invoice_{id}"))
                     .ReturnsAsync((InvoiceDto?)null);
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((InvoiceDto?)null);

        // Act
        var result = await _sut.GetById(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_ServiceThrows_Returns500()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<InvoiceDto>($"invoice_{id}"))
                     .ReturnsAsync((InvoiceDto?)null);
        _invoiceService.Setup(s => s.GetInvoiceByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("unexpected"));

        // Act
        var result = await _sut.GetById(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    // ─── GetByCustomer ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCustomer_CacheHit_SkipsService()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cached = SampleInvoiceList();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>($"invoices_customer_{customerId}"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetByCustomer(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _invoiceService.Verify(s => s.GetInvoicesByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByCustomer_CacheMiss_CallsService()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var invoices = SampleInvoiceList();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>($"invoices_customer_{customerId}"))
                     .ReturnsAsync((IEnumerable<InvoiceDto>?)null);
        _invoiceService.Setup(s => s.GetInvoicesByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetByCustomer(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _invoiceService.Verify(s => s.GetInvoicesByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── GetByOrder ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByOrder_CacheHit_SkipsService()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cached = SampleInvoiceList();
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>($"invoices_order_{orderId}"))
                     .ReturnsAsync(cached);

        // Act
        var result = await _sut.GetByOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _invoiceService.Verify(s => s.GetInvoicesByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrder_CacheMiss_CallsService()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var invoices = SampleInvoiceList(1);
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<InvoiceDto>>($"invoices_order_{orderId}"))
                     .ReturnsAsync((IEnumerable<InvoiceDto>?)null);
        _invoiceService.Setup(s => s.GetInvoicesByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(invoices);

        // Act
        var result = await _sut.GetByOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _invoiceService.Verify(s => s.GetInvoicesByOrderIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ValidQuery_ReturnsPaginatedResult()
    {
        // Arrange
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.HttpContext.Request.QueryString = new QueryString("?searchTerm=INV&page=1&pageSize=20");

        var query = new QuerySpec { Page = 1, PageSize = 20, SearchTerm = "INV" };
        var paginated = new PaginatedResult<InvoiceDto>(SampleInvoiceList(2), 1, 20, 2);
        _invoiceService.Setup(s => s.QueryInvoicesAsync(It.IsAny<ISpecification<Invoice>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        // Act
        var result = await _sut.Search(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(paginated);
        _invoiceService.Verify(s => s.QueryInvoicesAsync(It.IsAny<ISpecification<Invoice>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var query = new QuerySpec();
        _sut.ModelState.AddModelError("Page", "Invalid page");

        // Act
        var result = await _sut.Search(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _invoiceService.Verify(s => s.QueryInvoicesAsync(It.IsAny<ISpecification<Invoice>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_InvalidQuerySpec_ReturnsBadRequest()
    {
        // Arrange
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=20");

        var query = new QuerySpec();
        _invoiceService.Setup(s => s.QueryInvoicesAsync(It.IsAny<ISpecification<Invoice>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid query"));

        // Act
        var result = await _sut.Search(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            "INV-TEST-" + Guid.NewGuid().ToString()[..8], // InvoiceNumber
            Guid.NewGuid(),
            null,
            "USD",
            new List<CreateInvoiceLineDto> { new("Item", 1, 100m, 10m, 0m) },
            30);
        var created = SampleInvoiceDto("Draft");
        _invoiceService.Setup(s => s.CreateInvoiceAsync(dto, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_InvalidModel_Returns400()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            "asd", // InvoiceNumber
            Guid.NewGuid(),
            null,
            "USD",
            new List<CreateInvoiceLineDto>(),
            30);
        _sut.ModelState.AddModelError("Lines", "At least one line required");

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _invoiceService.Verify(s => s.CreateInvoiceAsync(It.IsAny<CreateInvoiceDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ServiceThrowsInvalidOperation_Returns400()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            "INV-TEST-" + Guid.NewGuid().ToString()[..8], // InvoiceNumber
            Guid.NewGuid(),
            null,
            "USD",
            new List<CreateInvoiceLineDto> { new("Item", 1, 100m, 10m, 0m) },
            30);
        _invoiceService.Setup(s => s.CreateInvoiceAsync(dto, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Customer blocked"));

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_InvalidatesOpenInvoicesCache()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            "INV-TEST-" + Guid.NewGuid().ToString()[..8], // InvoiceNumber
            Guid.NewGuid(),
            null,
            "USD",
            new List<CreateInvoiceLineDto> { new("Item", 1, 100m, 10m, 0m) },
            30);
        _invoiceService.Setup(s => s.CreateInvoiceAsync(dto, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(SampleInvoiceDto("Draft"));
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _sut.Create(dto, CancellationToken.None);

        // Assert
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
    }

    // ─── Issue ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Issue_ValidRequest_Returns200()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new IssueInvoiceRequest("INV-999", DateTime.UtcNow);
        var issued = SampleInvoiceDto("Issued");
        _invoiceService.Setup(s => s.IssueInvoiceAsync(id, request.InvoiceNumber, request.IssueDate, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(issued);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Issue(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().Be(issued);
    }

    [Fact]
    public async Task Issue_InvalidModel_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new IssueInvoiceRequest("", DateTime.UtcNow);
        _sut.ModelState.AddModelError("InvoiceNumber", "Required");

        // Act
        var result = await _sut.Issue(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _invoiceService.Verify(s => s.IssueInvoiceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Issue_InvoiceNotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new IssueInvoiceRequest("INV-X", DateTime.UtcNow);
        _invoiceService.Setup(s => s.IssueInvoiceAsync(id, request.InvoiceNumber, request.IssueDate, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception($"Invoice {id} not found"));

        // Act
        var result = await _sut.Issue(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Issue_ServiceThrowsInvalidOperation_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new IssueInvoiceRequest("INV-X", DateTime.UtcNow);
        _invoiceService.Setup(s => s.IssueInvoiceAsync(id, request.InvoiceNumber, request.IssueDate, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Already issued"));

        // Act
        var result = await _sut.Issue(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── RecordPayment ────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordPayment_ValidDto_Returns200()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RecordPaymentDto(id, 110m, "Card", DateTime.UtcNow);
        var updated = SampleInvoiceDto("Paid");
        _invoiceService.Setup(s => s.RecordPaymentAsync(It.IsAny<RecordPaymentDto>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(updated);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RecordPayment(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().Be(updated);
    }

    [Fact]
    public async Task RecordPayment_EnforcesInvoiceIdFromRoute()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var dto = new RecordPaymentDto(Guid.NewGuid(), 50m, "Cash", DateTime.UtcNow); // different ID
        var updated = SampleInvoiceDto("Issued");
        RecordPaymentDto? capturedDto = null;

        _invoiceService.Setup(s => s.RecordPaymentAsync(It.IsAny<RecordPaymentDto>(), It.IsAny<CancellationToken>()))
                       .Callback<RecordPaymentDto, CancellationToken>((d, _) => capturedDto = d)
                       .ReturnsAsync(updated);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _sut.RecordPayment(routeId, dto, CancellationToken.None);

        // Assert – route ID must win over body ID
        capturedDto!.InvoiceId.Should().Be(routeId);
    }

    [Fact]
    public async Task RecordPayment_InvoiceNotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RecordPaymentDto(id, 110m, "Card", DateTime.UtcNow);
        _invoiceService.Setup(s => s.RecordPaymentAsync(It.IsAny<RecordPaymentDto>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception($"Invoice {id} not found"));

        // Act
        var result = await _sut.RecordPayment(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RecordPayment_ServiceThrowsInvalidOperation_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RecordPaymentDto(id, 999m, "Card", DateTime.UtcNow);
        _invoiceService.Setup(s => s.RecordPaymentAsync(It.IsAny<RecordPaymentDto>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Overpayment not allowed"));

        // Act
        var result = await _sut.RecordPayment(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ValidRequest_Returns200()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CancelInvoiceRequest("Customer withdrew");
        var cancelled = SampleInvoiceDto("Cancelled");
        _invoiceService.Setup(s => s.CancelInvoiceAsync(id, request.Reason, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cancelled);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Cancel(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().Be(cancelled);
    }

    [Fact]
    public async Task Cancel_PaidInvoice_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CancelInvoiceRequest("Oops");
        _invoiceService.Setup(s => s.CancelInvoiceAsync(id, request.Reason, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Cannot cancel a paid invoice"));

        // Act
        var result = await _sut.Cancel(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_InvoiceNotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CancelInvoiceRequest("reason");
        _invoiceService.Setup(s => s.CancelInvoiceAsync(id, request.Reason, It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception($"Invoice {id} not found"));

        // Act
        var result = await _sut.Cancel(id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── CreateCreditNote ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCreditNote_ValidDto_Returns201Created()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateCreditNoteDto(id,
            new List<CreditNoteLineDto> { new("Refund", 1, 50m, 10m, 0m) },
            "Returned goods");
        var creditNote = SampleCreditNoteDto();
        _invoiceService.Setup(s => s.CreateCreditNoteAsync(It.IsAny<CreateCreditNoteDto>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(creditNote);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateCreditNote(id, dto, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(creditNote);
    }

    [Fact]
    public async Task CreateCreditNote_EnforcesInvoiceIdFromRoute()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var dto = new CreateCreditNoteDto(Guid.NewGuid(), // different ID in body
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) }, "test");
        CreateCreditNoteDto? captured = null;
        _invoiceService.Setup(s => s.CreateCreditNoteAsync(It.IsAny<CreateCreditNoteDto>(), It.IsAny<CancellationToken>()))
                       .Callback<CreateCreditNoteDto, CancellationToken>((d, _) => captured = d)
                       .ReturnsAsync(SampleCreditNoteDto());
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateCreditNote(routeId, dto, CancellationToken.None);

        // Assert
        captured!.InvoiceId.Should().Be(routeId);
    }

    [Fact]
    public async Task CreateCreditNote_InvoiceNotFound_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateCreditNoteDto(id,
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) }, "test");
        _invoiceService.Setup(s => s.CreateCreditNoteAsync(It.IsAny<CreateCreditNoteDto>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception($"Invoice {id} not found"));

        // Act
        var result = await _sut.CreateCreditNote(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateCreditNote_DraftInvoice_Returns400()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateCreditNoteDto(id,
            new List<CreditNoteLineDto> { new("X", 1, 10m, 0m, 0m) }, "test");
        _invoiceService.Setup(s => s.CreateCreditNoteAsync(It.IsAny<CreateCreditNoteDto>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Cannot create credit note for draft invoice"));

        // Act
        var result = await _sut.CreateCreditNote(id, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Cache invalidation ───────────────────────────────────────────────────

    [Fact]
    public async Task Issue_InvalidatesAllRelatedCacheKeys()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var request = new IssueInvoiceRequest("INV-42", DateTime.UtcNow);
        var dto = SampleInvoiceDto("Issued") with { Id = id, CustomerId = customerId, OrderId = orderId };

        _invoiceService.Setup(s => s.IssueInvoiceAsync(id, request.InvoiceNumber, request.IssueDate, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(dto);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _sut.Issue(id, request, CancellationToken.None);

        // Assert
        _cacheService.Verify(c => c.RemoveStateAsync($"invoice_{id}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoices_customer_{customerId}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync($"invoices_order_{orderId}"), Times.Once);
    }

    [Fact]
    public async Task Cancel_InvalidatesAllRelatedCacheKeys()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var request = new CancelInvoiceRequest("reason");
        var dto = SampleInvoiceDto("Cancelled") with { Id = id, CustomerId = customerId, OrderId = null };

        _invoiceService.Setup(s => s.CancelInvoiceAsync(id, request.Reason, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(dto);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _sut.Cancel(id, request, CancellationToken.None);

        // Assert
        _cacheService.Verify(c => c.RemoveStateAsync($"invoice_{id}"), Times.Once);
        _cacheService.Verify(c => c.RemoveStateAsync("open_invoices"), Times.Once);
        // No order cache when OrderId is null
        _cacheService.Verify(c => c.RemoveStateAsync(It.Is<string>(k => k.StartsWith("invoices_order_"))), Times.Never);
    }
}
