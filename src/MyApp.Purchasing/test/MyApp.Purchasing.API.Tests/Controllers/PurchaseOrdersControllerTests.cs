using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Purchasing.API.Controllers;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Purchasing.API.Tests.Controllers;

/// <summary>Unit tests for <see cref="PurchaseOrdersController"/> covering happy paths, NotFound, and key status codes.</summary>
public class PurchaseOrdersControllerTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderService;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<PurchaseOrdersController>> _logger;
    private readonly PurchaseOrdersController _sut;

    public PurchaseOrdersControllerTests()
    {
        _purchaseOrderService = new Mock<IPurchaseOrderService>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<PurchaseOrdersController>>();
        _sut = new PurchaseOrdersController(_purchaseOrderService.Object, _cacheService.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static PurchaseOrderDto SampleOrder(Guid? id = null, string orderNumber = "PO-001") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            OrderNumber = orderNumber,
            SupplierId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Draft,
            TotalAmount = 250m,
            Lines = []
        };

    private static CreateUpdatePurchaseOrderDto SampleCreateDto() =>
        new()
        {
            SupplierId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = 0,
            TotalAmount = 100m,
            Lines =
            [
                new CreateUpdatePurchaseOrderLineDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 100m
                }
            ]
        };

    // ─── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoQuery_CacheHit_ReturnsCached()
    {
        var cached = new List<PurchaseOrderDto> { SampleOrder() };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PurchaseOrderDto>>("all_purchase_orders"))
            .ReturnsAsync(cached);

        var result = await _sut.GetAllPurchaseOrders(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _purchaseOrderService.Verify(s => s.GetAllPurchaseOrdersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_NoQuery_CacheMiss_Returns200()
    {
        var orders = new List<PurchaseOrderDto> { SampleOrder() };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<PurchaseOrderDto>>("all_purchase_orders"))
            .ReturnsAsync((IEnumerable<PurchaseOrderDto>?)null);
        _purchaseOrderService.Setup(s => s.GetAllPurchaseOrdersAsync())
            .ReturnsAsync(orders);
        _cacheService.Setup(c => c.SaveStateAsync("all_purchase_orders", orders, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetAllPurchaseOrders(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(orders);
    }

    [Fact]
    public async Task GetAll_WithQueryParams_QueriesService()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");
        var paginated = new PaginatedResult<PurchaseOrderDto>(new List<PurchaseOrderDto> { SampleOrder() }, 1, 10, 1);
        _purchaseOrderService.Setup(s => s.QueryPurchaseOrdersAsync(It.IsAny<ISpecification<PurchaseOrder>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _sut.GetAllPurchaseOrders(new QuerySpec { Page = 1, PageSize = 10 });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(paginated);
    }

    // ─── GetById / GetByOrderNumber ──────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheHit_Returns200()
    {
        var id = Guid.NewGuid();
        var cached = SampleOrder(id);
        _cacheService.Setup(c => c.GetStateAsync<PurchaseOrderDto>($"PurchaseOrder-{id}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetPurchaseOrderById(id);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _purchaseOrderService.Verify(s => s.GetPurchaseOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<PurchaseOrderDto>($"PurchaseOrder-{id}"))
            .ReturnsAsync((PurchaseOrderDto?)null);
        _purchaseOrderService.Setup(s => s.GetPurchaseOrderByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderDto?)null);

        var result = await _sut.GetPurchaseOrderById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByOrderNumber_Exists_Returns200()
    {
        var order = SampleOrder(orderNumber: "PO-99");
        _cacheService.Setup(c => c.GetStateAsync<PurchaseOrderDto>("PurchaseOrder-Code-PO-99"))
            .ReturnsAsync((PurchaseOrderDto?)null);
        _purchaseOrderService.Setup(s => s.GetPurchaseOrderByOrderNumberAsync("PO-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cacheService.Setup(c => c.SaveStateAsync("PurchaseOrder-Code-PO-99", order, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetPurchaseOrderByOrderNumber("PO-99");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetByOrderNumber_NotFound_Returns404()
    {
        _cacheService.Setup(c => c.GetStateAsync<PurchaseOrderDto>("PurchaseOrder-Code-MISSING"))
            .ReturnsAsync((PurchaseOrderDto?)null);
        _purchaseOrderService.Setup(s => s.GetPurchaseOrderByOrderNumberAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderDto?)null);

        var result = await _sut.GetPurchaseOrderByOrderNumber("MISSING");

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ─── Create / Update / Delete ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = SampleCreateDto();
        var created = SampleOrder();
        _purchaseOrderService.Setup(s => s.CreatePurchaseOrderAsync(dto)).ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync("all_purchase_orders")).Returns(Task.CompletedTask);

        var result = await _sut.CreatePurchaseOrder(dto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_NotFound_Returns404()
    {
        var dto = SampleCreateDto();
        _purchaseOrderService.Setup(s => s.CreatePurchaseOrderAsync(dto))
            .ThrowsAsync(new KeyNotFoundException("Supplier not found"));

        var result = await _sut.CreatePurchaseOrder(dto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_InvalidModel_Returns400()
    {
        _sut.ModelState.AddModelError("SupplierId", "Required");

        var result = await _sut.CreatePurchaseOrder(SampleCreateDto());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = SampleCreateDto();
        _purchaseOrderService.Setup(s => s.UpdatePurchaseOrderAsync(id, dto))
            .ThrowsAsync(new KeyNotFoundException("PO not found"));

        var result = await _sut.UpdatePurchaseOrder(id, dto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService.Setup(s => s.DeletePurchaseOrderAsync(id)).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.DeletePurchaseOrder(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService.Setup(s => s.DeletePurchaseOrderAsync(id))
            .ThrowsAsync(new KeyNotFoundException("PO not found"));

        var result = await _sut.DeletePurchaseOrder(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Status / Approve / Match ────────────────────────────────────────────

    [Fact]
    public async Task GetByStatus_InvalidStatus_Returns400()
    {
        var result = await _sut.GetPurchaseOrdersByStatus("NotARealStatus");

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_Valid_Returns200()
    {
        var orders = new List<PurchaseOrderDto> { SampleOrder() };
        _purchaseOrderService.Setup(s => s.GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var result = await _sut.GetPurchaseOrdersByStatus("Draft");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(orders);
    }

    [Fact]
    public async Task Approve_IdMismatch_Returns400()
    {
        var id = Guid.NewGuid();
        var dto = new ApprovePurchaseOrderDto { PurchaseOrderId = Guid.NewGuid() };

        var result = await _sut.ApprovePurchaseOrder(id, dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _purchaseOrderService.Verify(s => s.ApprovePurchaseOrderAsync(It.IsAny<ApprovePurchaseOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task Approve_Valid_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = new ApprovePurchaseOrderDto { PurchaseOrderId = id };
        var approved = SampleOrder(id);
        _purchaseOrderService.Setup(s => s.ApprovePurchaseOrderAsync(dto)).ReturnsAsync(approved);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.ApprovePurchaseOrder(id, dto);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(approved);
    }

    [Fact]
    public async Task Match_InvalidOperation_Returns400()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService.Setup(s => s.MatchPurchaseOrderAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot match"));

        var result = await _sut.MatchPurchaseOrder(id, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Match_Valid_Returns200()
    {
        var id = Guid.NewGuid();
        var matchResult = new PurchaseOrderMatchResultDto(id, PurchaseOrderMatchStatus.Matched, DateTime.UtcNow, "OK");
        _purchaseOrderService.Setup(s => s.MatchPurchaseOrderAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.MatchPurchaseOrder(id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(matchResult);
    }
}
