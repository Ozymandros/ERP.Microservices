using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Sales.API.Controllers;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Sales.API.Tests.Controllers;

/// <summary>Unit tests for <see cref="SalesOrdersController"/> covering happy paths, NotFound, and key status codes.</summary>
public class SalesOrdersControllerTests
{
    private readonly Mock<ISalesOrderService> _salesOrderService;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<SalesOrdersController>> _logger;
    private readonly SalesOrdersController _sut;

    public SalesOrdersControllerTests()
    {
        _salesOrderService = new Mock<ISalesOrderService>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<SalesOrdersController>>();
        _sut = new SalesOrdersController(_salesOrderService.Object, _cacheService.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static SalesOrderDto SampleOrder(Guid? id = null, string orderNumber = "SO-001") =>
        new(id ?? Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = 1,
            TotalAmount = 100m
        };

    private static CreateUpdateSalesOrderDto SampleCreateDto() =>
        new(Guid.NewGuid(), DateTime.UtcNow, null, 0, 50m,
            new List<CreateUpdateSalesOrderLineDto> { new(Guid.NewGuid(), 1, 50m) });

    // ─── GetAll / Search ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ValidQuery_Returns200()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=20");
        var paginated = new PaginatedResult<SalesOrderDto>(new List<SalesOrderDto> { SampleOrder() }, 1, 20, 1);
        _salesOrderService.Setup(s => s.QuerySalesOrdersAsync(It.IsAny<ISpecification<MyApp.Sales.Domain.Entities.SalesOrder>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _sut.GetAll(new QuerySpec { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(paginated);
    }

    [Fact]
    public async Task GetAll_InvalidModel_Returns400()
    {
        _sut.ModelState.AddModelError("Page", "Invalid");

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ArgumentException_Returns400()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=20");
        _salesOrderService.Setup(s => s.QuerySalesOrdersAsync(It.IsAny<ISpecification<MyApp.Sales.Domain.Entities.SalesOrder>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid query"));

        var result = await _sut.Search(new QuerySpec { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── GetById / GetByOrderNumber ──────────────────────────────────────────

    [Fact]
    public async Task GetById_CacheHit_Returns200()
    {
        var id = Guid.NewGuid();
        var cached = SampleOrder(id);
        _cacheService.Setup(c => c.GetStateAsync<SalesOrderDto>($"SalesOrder-{id}"))
            .ReturnsAsync(cached);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(cached);
        _salesOrderService.Verify(s => s.GetSalesOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<SalesOrderDto>($"SalesOrder-{id}"))
            .ReturnsAsync((SalesOrderDto?)null);
        _salesOrderService.Setup(s => s.GetSalesOrderByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrderDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByOrderNumber_Exists_Returns200()
    {
        var order = SampleOrder(orderNumber: "SO-99");
        _cacheService.Setup(c => c.GetStateAsync<SalesOrderDto>("SalesOrder-Code-SO-99"))
            .ReturnsAsync((SalesOrderDto?)null);
        _salesOrderService.Setup(s => s.GetSalesOrderByOrderNumberAsync("SO-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cacheService.Setup(c => c.SaveStateAsync("SalesOrder-Code-SO-99", order, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetByOrderNumber("SO-99", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetByOrderNumber_NotFound_Returns404()
    {
        _cacheService.Setup(c => c.GetStateAsync<SalesOrderDto>("SalesOrder-Code-MISSING"))
            .ReturnsAsync((SalesOrderDto?)null);
        _salesOrderService.Setup(s => s.GetSalesOrderByOrderNumberAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrderDto?)null);

        var result = await _sut.GetByOrderNumber("MISSING", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Create / Update / Delete ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = SampleCreateDto();
        var created = SampleOrder();
        _salesOrderService.Setup(s => s.CreateSalesOrderAsync(dto)).ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync("all_sales_orders")).Returns(Task.CompletedTask);

        var result = await _sut.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_InvalidOperation_Returns400()
    {
        var dto = SampleCreateDto();
        _salesOrderService.Setup(s => s.CreateSalesOrderAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Customer blocked"));

        var result = await _sut.Create(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = SampleCreateDto();
        _salesOrderService.Setup(s => s.UpdateSalesOrderAsync(id, dto))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        var id = Guid.NewGuid();
        _salesOrderService.Setup(s => s.DeleteSalesOrderAsync(id)).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _salesOrderService.Setup(s => s.DeleteSalesOrderAsync(id))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Quotes / Returns ────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmQuote_IdMismatch_Returns400()
    {
        var id = Guid.NewGuid();
        var dto = new ConfirmQuoteDto { QuoteId = Guid.NewGuid(), WarehouseId = Guid.NewGuid() };

        var result = await _sut.ConfirmQuote(id, dto);

        result.Should().BeOfType<BadRequestObjectResult>();
        _salesOrderService.Verify(s => s.ConfirmQuoteAsync(It.IsAny<ConfirmQuoteDto>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmQuote_Valid_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = new ConfirmQuoteDto { QuoteId = id, WarehouseId = Guid.NewGuid() };
        var confirmed = SampleOrder(id);
        _salesOrderService.Setup(s => s.ConfirmQuoteAsync(dto)).ReturnsAsync(confirmed);
        _cacheService.Setup(c => c.RemoveStateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.ConfirmQuote(id, dto);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(confirmed);
    }

    [Fact]
    public async Task CreateReturn_InvalidOperation_Returns400()
    {
        var id = Guid.NewGuid();
        var dto = new CreateSalesReturnDto
        {
            WarehouseId = Guid.NewGuid(),
            Lines = [new CreateSalesReturnLineDto(Guid.NewGuid(), 1)]
        };
        _salesOrderService.Setup(s => s.CreateReturnAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot return"));

        var result = await _sut.CreateReturn(id, dto, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
