using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Services;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Constants;
using MyApp.Inventory.Application.Contracts.DTOs;
using Xunit;

namespace MyApp.Orders.Application.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IOrderLineRepository> _mockOrderLineRepository;
    private readonly Mock<IReservedStockRepository> _mockReservedStockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IServiceInvoker> _mockServiceInvoker;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockOrderLineRepository = new Mock<IOrderLineRepository>();
        _mockReservedStockRepository = new Mock<IReservedStockRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<OrderService>>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockServiceInvoker = new Mock<IServiceInvoker>();

        _orderService = new OrderService(
            _mockOrderRepository.Object,
            _mockOrderLineRepository.Object,
            _mockReservedStockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockEventPublisher.Object,
            _mockServiceInvoker.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CallsAddAsyncOnRepository()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-001",
            Type = OrderType.Transfer,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>
            {
                new CreateOrderLineDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 5
                }
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-001",
            Type = OrderType.Transfer,
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 5 }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid())
            {
                OrderDate = DateTime.UtcNow,
                OrderNumber = "ORD-001",
                Type = "Transfer",
                Status = "Draft",
                Lines = new List<OrderLineDto>()
            });

        // Act
        var result = await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Draft &&
            o.Type == OrderType.Transfer &&
            o.Id != Guid.Empty &&
            o.Lines.All(l => l.OrderId == o.Id && l.Id != Guid.Empty)
        )), Times.Once);

        _mockMapper.Verify(m => m.Map<Order>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<OrderDto>(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsOrderStatusToDraft()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-002",
            Type = OrderType.Inbound,
            TargetId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>
            {
                new CreateOrderLineDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid())
            {
                OrderDate = DateTime.UtcNow,
                OrderNumber = "ORD-002",
                Type = "Inbound",
                Status = "Draft"
            });

        // Act
        await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Draft
        )), Times.Once);
    }

    #endregion

    #region CreateOrderWithReservationAsync Tests

    [Fact]
    public async Task CreateOrderWithReservationAsync_WithValidDto_ReservesStockAndCreatesOrder()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var dto = new CreateOrderWithReservationDto
        {
            OrderNumber = "ORD-RES-001",
            Type = OrderType.Outbound,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>
            {
                new CreateOrderLineDto { ProductId = productId, Quantity = 5 }
            }
        };

        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = dto.OrderNumber,
            Type = dto.Type,
            WarehouseId = dto.WarehouseId,
            Lines = new List<OrderLine>()
        };

        var line = new OrderLine(Guid.NewGuid())
        {
            OrderId = order.Id,
            ProductId = productId,
            Quantity = 5
        };

        var reservationId = Guid.NewGuid();
        var reservationResponse = new ReservationDto(reservationId)
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 5,
            ReservedUntil = DateTime.UtcNow.AddDays(1),
            Status = "Reserved"
        };
        
        _mockServiceInvoker.Setup(s => s.InvokeAsync<ReserveStockDto, ReservationDto>(
            ServiceNames.Inventory,
            ApiEndpoints.Inventory.ReserveStock,
            HttpMethod.Post,
            It.IsAny<ReserveStockDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservationResponse);

        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>())).Returns(new OrderDto(Guid.NewGuid()));

        // Act
        var result = await _orderService.CreateOrderWithReservationAsync(dto);

        // Assert
        Assert.NotNull(result);
        _mockServiceInvoker.Verify(s => s.InvokeAsync<ReserveStockDto, ReservationDto>(
            ServiceNames.Inventory,
            ApiEndpoints.Inventory.ReserveStock,
            HttpMethod.Post,
            It.IsAny<ReserveStockDto>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockReservedStockRepository.Verify(r => r.AddAsync(It.IsAny<ReservedStock>()), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            OrderNumber = "ORD-003",
            Type = OrderType.Outbound,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Approved
        };

        var expectedDto = new OrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "ORD-003",
            Type = "Outbound",
            Status = "Approved"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<OrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _orderService.GetByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORD-003", result.OrderNumber);
        Assert.Equal("Approved", result.Status);
        Assert.Equal("Outbound", result.Type);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockMapper.Verify(m => m.Map<OrderDto>(order), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_CallsRepositoryDeleteWithCorrectId()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        await _orderService.DeleteAsync(orderId);

        // Assert
        _mockOrderRepository.Verify(r => r.DeleteAsync(orderId), Times.Once);
    }

    #endregion
}
