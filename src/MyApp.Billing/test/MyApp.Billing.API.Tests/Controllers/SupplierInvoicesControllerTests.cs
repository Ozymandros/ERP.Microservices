using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyApp.Billing.API.Controllers;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using Xunit;

namespace MyApp.Billing.API.Tests.Controllers;

/// <summary>Unit tests for <see cref="SupplierInvoicesController"/> covering get, list-by-PO, and create endpoints.</summary>
public class SupplierInvoicesControllerTests
{
    private readonly Mock<ISupplierInvoiceService> _service;
    private readonly Mock<ICacheService> _cacheService;
    private readonly SupplierInvoicesController _sut;

    public SupplierInvoicesControllerTests()
    {
        _service = new Mock<ISupplierInvoiceService>();
        _cacheService = new Mock<ICacheService>();
        _sut = new SupplierInvoicesController(_service.Object, _cacheService.Object);
    }

    private static SupplierInvoiceDto SampleDto(Guid? id = null, Guid? purchaseOrderId = null) =>
        new(
            id ?? Guid.NewGuid(),
            "SI-001",
            Guid.NewGuid(),
            purchaseOrderId ?? Guid.NewGuid(),
            "USD",
            new List<SupplierInvoiceLineDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), 2, 25m)
            },
            DateTime.UtcNow,
            DateTime.UtcNow);

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheHit_ReturnsCachedInvoice()
    {
        var id = Guid.NewGuid();
        var cached = SampleDto(id);
        _cacheService.Setup(c => c.GetStateAsync<SupplierInvoiceDto>($"supplier_invoice_{id}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _service.Verify(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheService.Verify(c => c.SaveStateAsync($"supplier_invoice_{id}", cached, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CacheMiss_InvoiceExists_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _cacheService.Setup(c => c.GetStateAsync<SupplierInvoiceDto>($"supplier_invoice_{id}"))
            .ReturnsAsync((SupplierInvoiceDto?)null);
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        _cacheService.Setup(c => c.SaveStateAsync($"supplier_invoice_{id}", dto, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
        _cacheService.Verify(c => c.SaveStateAsync($"supplier_invoice_{id}", dto, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<SupplierInvoiceDto>($"supplier_invoice_{id}"))
            .ReturnsAsync((SupplierInvoiceDto?)null);
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierInvoiceDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
        _cacheService.Verify(c => c.SaveStateAsync(It.IsAny<string>(), It.IsAny<SupplierInvoiceDto>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    // ─── GetByPurchaseOrder ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByPurchaseOrder_CacheHit_SkipsService()
    {
        var poId = Guid.NewGuid();
        var cached = new List<SupplierInvoiceDto> { SampleDto(purchaseOrderId: poId) };
        _cacheService.Setup(c => c.GetStateAsync<List<SupplierInvoiceDto>>($"supplier_invoices_po_{poId}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetByPurchaseOrder(poId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _service.Verify(s => s.GetByPurchaseOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByPurchaseOrder_CacheMiss_WithResults_CachesAndReturns200()
    {
        var poId = Guid.NewGuid();
        var invoices = new List<SupplierInvoiceDto> { SampleDto(purchaseOrderId: poId) };
        _cacheService.Setup(c => c.GetStateAsync<List<SupplierInvoiceDto>>($"supplier_invoices_po_{poId}"))
            .ReturnsAsync((List<SupplierInvoiceDto>?)null);
        _service.Setup(s => s.GetByPurchaseOrderIdAsync(poId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);
        _cacheService.Setup(c => c.SaveStateAsync($"supplier_invoices_po_{poId}", invoices, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetByPurchaseOrder(poId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(invoices);
        _cacheService.Verify(c => c.SaveStateAsync($"supplier_invoices_po_{poId}", invoices, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetByPurchaseOrder_CacheMiss_EmptyList_DoesNotCache()
    {
        var poId = Guid.NewGuid();
        var empty = new List<SupplierInvoiceDto>();
        _cacheService.Setup(c => c.GetStateAsync<List<SupplierInvoiceDto>>($"supplier_invoices_po_{poId}"))
            .ReturnsAsync((List<SupplierInvoiceDto>?)null);
        _service.Setup(s => s.GetByPurchaseOrderIdAsync(poId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        var result = await _sut.GetByPurchaseOrder(poId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(empty);
        _cacheService.Verify(c => c.SaveStateAsync(It.IsAny<string>(), It.IsAny<List<SupplierInvoiceDto>>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201AndInvalidatesPoCache()
    {
        var poId = Guid.NewGuid();
        var dto = new CreateSupplierInvoiceDto(
            "SI-NEW",
            Guid.NewGuid(),
            poId,
            "USD",
            new List<CreateSupplierInvoiceLineDto> { new(Guid.NewGuid(), 1, 10m) });
        var created = SampleDto(purchaseOrderId: poId);
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync($"supplier_invoices_po_{poId}"))
            .Returns(Task.CompletedTask);

        var result = await _sut.Create(dto, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SupplierInvoicesController.GetById));
        createdResult.Value.Should().Be(created);
        _cacheService.Verify(c => c.RemoveStateAsync($"supplier_invoices_po_{poId}"), Times.Once);
    }
}
