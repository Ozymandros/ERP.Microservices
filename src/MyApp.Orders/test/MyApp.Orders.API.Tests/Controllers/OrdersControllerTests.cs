using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Orders.API;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Orders.API.Tests.Controllers;

/// <summary>Unit tests for <see cref="OrdersController"/> covering happy paths, NotFound, and key status codes.</summary>
public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _orderService;
    private readonly Mock<ICacheService> _cacheService;
    private readonly Mock<ILogger<OrdersController>> _logger;
    private readonly OrdersController _sut;

    public OrdersControllerTests()
    {
        _orderService = new Mock<IOrderService>();
        _cacheService = new Mock<ICacheService>();
        _logger = new Mock<ILogger<OrdersController>>();
        _sut = new OrdersController(_orderService.Object, _cacheService.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static OrderDto SampleOrder(Guid? id = null, string orderNumber = "ORD-001") =>
        new(id ?? Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            Status = "Created",
            Type = "Outbound"
        };

    private static CreateUpdateOrderDto SampleCreateDto() =>
        new()
        {
            OrderNumber = "ORD-NEW",
            OrderDate = DateTime.UtcNow,
            Type = OrderType.Outbound,
            SourceId = Guid.NewGuid(),
            Lines = [new CreateOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 2 }]
        };

    // ─── GetAll / Search ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoQuery_Returns200()
    {
        var orders = new List<OrderDto> { SampleOrder(), SampleOrder() };
        _orderService.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        var result = await _sut.GetAll(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(orders);
    }

    [Fact]
    public async Task GetAll_WithQueryParams_QueriesService()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10");
        var paginated = new PaginatedResult<OrderDto>(new List<OrderDto> { SampleOrder() }, 1, 10, 1);
        _orderService.Setup(s => s.QueryOrdersAsync(It.IsAny<ISpecification<Order>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _sut.GetAll(new QuerySpec { Page = 1, PageSize = 10 });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(paginated);
    }

    [Fact]
    public async Task Search_ArgumentException_Returns400()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=20");
        _orderService.Setup(s => s.QueryOrdersAsync(It.IsAny<ISpecification<Order>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid query"));

        var result = await _sut.Search(new QuerySpec { Page = 1, PageSize = 20 });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── GetById / GetByOrderNumber ──────────────────────────────────────────

    [Fact]
    public async Task GetById_Exists_Returns200()
    {
        var id = Guid.NewGuid();
        var order = SampleOrder(id);
        _orderService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _orderService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDto)null!);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByOrderNumber_Exists_Returns200()
    {
        var order = SampleOrder(orderNumber: "ORD-42");
        _orderService.Setup(s => s.GetByOrderNumberAsync("ORD-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetByOrderNumber("ORD-42");

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetByOrderNumber_NotFound_Returns404()
    {
        _orderService.Setup(s => s.GetByOrderNumberAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDto?)null);

        var result = await _sut.GetByOrderNumber("MISSING");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Create / Update / Delete ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        var dto = SampleCreateDto();
        var created = SampleOrder();
        _orderService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);
        _cacheService.Setup(c => c.RemoveStateAsync("all_orders")).Returns(Task.CompletedTask);

        var result = await _sut.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().Be(created);
        _cacheService.Verify(c => c.RemoveStateAsync("all_orders"), Times.Once);
    }

    [Fact]
    public async Task Create_InvalidModel_Returns400()
    {
        _sut.ModelState.AddModelError("OrderNumber", "Required");

        var result = await _sut.Create(SampleCreateDto());

        result.Should().BeOfType<BadRequestObjectResult>();
        _orderService.Verify(s => s.CreateAsync(It.IsAny<CreateUpdateOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        var id = Guid.NewGuid();
        var dto = SampleCreateDto();
        _orderService.Setup(s => s.UpdateAsync(id, dto)).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveStateAsync("all_orders")).Returns(Task.CompletedTask);

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = SampleCreateDto();
        _orderService.Setup(s => s.UpdateAsync(id, dto))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        var id = Guid.NewGuid();
        _orderService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveStateAsync("all_orders")).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _orderService.Setup(s => s.DeleteAsync(id))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── Fulfill / Cancel ────────────────────────────────────────────────────

    [Fact]
    public async Task Fulfill_Valid_Returns200()
    {
        var id = Guid.NewGuid();
        var dto = new FulfillOrderDto { OrderId = Guid.Empty, WarehouseId = Guid.NewGuid() };
        var fulfilled = SampleOrder(id);
        _orderService.Setup(s => s.FulfillOrderAsync(It.Is<FulfillOrderDto>(d => d.OrderId == id)))
            .ReturnsAsync(fulfilled);

        var result = await _sut.Fulfill(id, dto);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(fulfilled);
    }

    [Fact]
    public async Task Fulfill_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = new FulfillOrderDto { WarehouseId = Guid.NewGuid() };
        _orderService.Setup(s => s.FulfillOrderAsync(It.IsAny<FulfillOrderDto>()))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Fulfill(id, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_Valid_Returns204()
    {
        var id = Guid.NewGuid();
        var dto = new CancelOrderDto { Reason = "Customer cancelled" };
        _orderService.Setup(s => s.CancelOrderAsync(It.Is<CancelOrderDto>(d => d.OrderId == id)))
            .Returns(Task.CompletedTask);

        var result = await _sut.Cancel(id, dto);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cancel_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var dto = new CancelOrderDto { Reason = "Oops" };
        _orderService.Setup(s => s.CancelOrderAsync(It.IsAny<CancelOrderDto>()))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        var result = await _sut.Cancel(id, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
