using AutoMapper;
using FluentAssertions;
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
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Shared.Domain.Exceptions;
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

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithExistingOrders_ReturnsListOfOrderDto()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-001", Type = OrderType.Outbound, Status = OrderStatus.Draft },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-002", Type = OrderType.Inbound, Status = OrderStatus.Approved }
        };
        var expectedDtos = orders.Select(o => new OrderDto(o.Id) { OrderNumber = o.OrderNumber, Type = o.Type.ToString(), Status = o.Status.ToString() }).ToList();

        _mockOrderRepository.Setup(r => r.ListAsync()).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(o => expectedDtos.First(d => d.Id == o.Id));

        // Act
        var result = await _orderService.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockOrderRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithNoOrders_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.ListAsync()).ReturnsAsync(new List<Order>());

        // Act
        var result = await _orderService.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockOrderRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingOrder_UpdatesOrderProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order(orderId)
        {
            OrderNumber = "ORD-OLD",
            Type = OrderType.Outbound,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Status = OrderStatus.Draft,
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 5 }
            }
        };
        var updateDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-NEW",
            Type = OrderType.Inbound,
            OrderDate = DateTime.UtcNow,
            TargetId = Guid.NewGuid(),
            Lines = new List<CreateOrderLineDto>
            {
                new CreateOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 10 }
            }
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        // Act
        await _orderService.UpdateAsync(orderId, updateDto);

        // Assert
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.OrderNumber == "ORD-NEW" &&
            o.Type == OrderType.Inbound &&
            o.TargetId == updateDto.TargetId &&
            o.Lines.Count == 1 &&
            o.Lines[0].Quantity == 10
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrder_DoesNothing()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-NEW",
            Type = OrderType.Outbound,
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        await _orderService.UpdateAsync(orderId, updateDto);

        // Assert
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    #endregion

    #region CreateOrderWithReservationAsync Tests - Additional Scenarios

    [Fact]
    public async Task CreateOrderWithReservationAsync_WithEmptyLines_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateOrderWithReservationDto
        {
            OrderNumber = "ORD-EMPTY",
            Type = OrderType.Outbound,
            WarehouseId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>()
        };

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderWithReservationAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least one line*");
    }

    [Fact]
    public async Task CreateOrderWithReservationAsync_WhenStockReservationFails_RollsBackOrder()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var dto = new CreateOrderWithReservationDto
        {
            OrderNumber = "ORD-FAIL",
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
            Status = OrderStatus.Draft,
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = productId, Quantity = 5 }
            }
        };

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask)
            .Callback<Order>(o => order = o);
        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => order);
        _mockServiceInvoker
            .Setup(s => s.InvokeAsync<ReserveStockDto, ReservationDto>(
                ServiceNames.Inventory,
                ApiEndpoints.Inventory.ReserveStock,
                HttpMethod.Post,
                It.IsAny<ReserveStockDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Stock reservation failed"));
        _mockReservedStockRepository
            .Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ReservedStock>());
        _mockServiceInvoker
            .Setup(s => s.InvokeAsync(
                ServiceNames.Inventory,
                It.IsAny<string>(),
                HttpMethod.Delete))
            .Returns(Task.CompletedTask);
        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderWithReservationAsync(dto);

        // Assert
        await act.Should().ThrowAsync<OrderFulfillmentException>();
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderWithReservationAsync_WithMultipleLines_ReservesStockForAllLines()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var dto = new CreateOrderWithReservationDto
        {
            OrderNumber = "ORD-MULTI",
            Type = OrderType.Outbound,
            WarehouseId = warehouseId,
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateOrderLineDto>
            {
                new CreateOrderLineDto { ProductId = productId1, Quantity = 5 },
                new CreateOrderLineDto { ProductId = productId2, Quantity = 10 }
            }
        };

        var reservation1 = new ReservationDto(Guid.NewGuid())
        {
            ProductId = productId1,
            WarehouseId = warehouseId,
            Quantity = 5,
            ReservedUntil = DateTime.UtcNow.AddDays(1),
            Status = "Reserved"
        };
        var reservation2 = new ReservationDto(Guid.NewGuid())
        {
            ProductId = productId2,
            WarehouseId = warehouseId,
            Quantity = 10,
            ReservedUntil = DateTime.UtcNow.AddDays(1),
            Status = "Reserved"
        };

        _mockServiceInvoker
            .SetupSequence(s => s.InvokeAsync<ReserveStockDto, ReservationDto>(
                ServiceNames.Inventory,
                ApiEndpoints.Inventory.ReserveStock,
                HttpMethod.Post,
                It.IsAny<ReserveStockDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation1)
            .ReturnsAsync(reservation2);

        _mockReservedStockRepository
            .Setup(r => r.AddAsync(It.IsAny<ReservedStock>()))
            .Returns(Task.CompletedTask);
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        _mockMapper
            .Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid()));

        // Act
        var result = await _orderService.CreateOrderWithReservationAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _mockServiceInvoker.Verify(s => s.InvokeAsync<ReserveStockDto, ReservationDto>(
            ServiceNames.Inventory,
            ApiEndpoints.Inventory.ReserveStock,
            HttpMethod.Post,
            It.IsAny<ReserveStockDto>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockReservedStockRepository.Verify(r => r.AddAsync(It.IsAny<ReservedStock>()), Times.Exactly(2));
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

    #region FulfillOrderAsync Tests

    [Fact]
    public async Task FulfillOrderAsync_WithValidOutboundOrder_FulfillsOrderAndReservations()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            OrderNumber = "ORD-FULFILL",
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved,
            WarehouseId = warehouseId,
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 5 }
            }
        };
        var reservations = new List<ReservedStock>
        {
            new ReservedStock(Guid.NewGuid())
            {
                OrderId = orderId,
                ProductId = order.Lines[0].ProductId,
                Quantity = 5,
                Status = ReservationStatus.Reserved
            }
        };
        var dto = new FulfillOrderDto
        {
            OrderId = orderId,
            WarehouseId = warehouseId,
            ShippingAddress = "123 Main St",
            TrackingNumber = "TRACK-001"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockReservedStockRepository.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(reservations);
        _mockReservedStockRepository.Setup(r => r.UpdateAsync(It.IsAny<ReservedStock>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>())).Returns(new OrderDto(orderId));

        // Act
        var result = await _orderService.FulfillOrderAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Completed &&
            o.FulfilledDate.HasValue &&
            o.TrackingNumber == "TRACK-001" &&
            o.Lines.All(l => l.IsFulfilled)
        )), Times.Once);
        _mockReservedStockRepository.Verify(r => r.UpdateAsync(It.Is<ReservedStock>(rs =>
            rs.Status == ReservationStatus.Fulfilled
        )), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task FulfillOrderAsync_WithNonExistentOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dto = new FulfillOrderDto
        {
            OrderId = orderId,
            WarehouseId = Guid.NewGuid()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _orderService.FulfillOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Order {orderId} not found*");
    }

    [Fact]
    public async Task FulfillOrderAsync_WithCompletedOrder_ThrowsOrderFulfillmentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            Status = OrderStatus.Completed
        };
        var dto = new FulfillOrderDto
        {
            OrderId = orderId,
            WarehouseId = Guid.NewGuid()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _orderService.FulfillOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<OrderFulfillmentException>()
            .WithMessage($"*cannot be fulfilled*");
    }

    [Fact]
    public async Task FulfillOrderAsync_WithOutboundOrderAndNoReservations_ThrowsOrderFulfillmentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved
        };
        var dto = new FulfillOrderDto
        {
            OrderId = orderId,
            WarehouseId = Guid.NewGuid()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockReservedStockRepository.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(new List<ReservedStock>());

        // Act
        Func<Task> act = async () => await _orderService.FulfillOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<OrderFulfillmentException>()
            .WithMessage("*No stock reservations found*");
    }

    [Fact]
    public async Task FulfillOrderAsync_WithNonReservedReservation_ThrowsOrderFulfillmentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved
        };
        var reservations = new List<ReservedStock>
        {
            new ReservedStock(Guid.NewGuid())
            {
                OrderId = orderId,
                Status = ReservationStatus.Cancelled
            }
        };
        var dto = new FulfillOrderDto
        {
            OrderId = orderId,
            WarehouseId = Guid.NewGuid()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockReservedStockRepository.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(reservations);

        // Act
        Func<Task> act = async () => await _orderService.FulfillOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<OrderFulfillmentException>()
            .WithMessage("*not in Reserved status*");
    }

    #endregion

    #region CancelOrderAsync Tests

    [Fact]
    public async Task CancelOrderAsync_WithValidOrder_CancelsOrderAndReleasesReservations()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            OrderNumber = "ORD-CANCEL",
            Status = OrderStatus.Approved
        };
        var reservations = new List<ReservedStock>
        {
            new ReservedStock(reservationId)
            {
                OrderId = orderId,
                Status = ReservationStatus.Reserved
            }
        };
        var dto = new CancelOrderDto
        {
            OrderId = orderId,
            Reason = "Customer request"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockReservedStockRepository.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(reservations);
        _mockServiceInvoker
            .Setup(s => s.InvokeAsync(
                ServiceNames.Inventory,
                It.IsAny<string>(),
                HttpMethod.Delete))
            .Returns(Task.CompletedTask);
        _mockReservedStockRepository.Setup(r => r.UpdateAsync(It.IsAny<ReservedStock>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        // Act
        await _orderService.CancelOrderAsync(dto);

        // Assert
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Cancelled
        )), Times.Once);
        _mockReservedStockRepository.Verify(r => r.UpdateAsync(It.Is<ReservedStock>(rs =>
            rs.Status == ReservationStatus.Cancelled
        )), Times.Once);
        _mockServiceInvoker.Verify(s => s.InvokeAsync(
            ServiceNames.Inventory,
            It.IsAny<string>(),
            HttpMethod.Delete), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WithNonExistentOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dto = new CancelOrderDto
        {
            OrderId = orderId,
            Reason = "Test"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _orderService.CancelOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Order {orderId} not found*");
    }

    [Fact]
    public async Task CancelOrderAsync_WithCompletedOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            Status = OrderStatus.Completed
        };
        var dto = new CancelOrderDto
        {
            OrderId = orderId,
            Reason = "Test"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _orderService.CancelOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Cannot cancel completed order*");
    }

    [Fact]
    public async Task CancelOrderAsync_WhenReservationReleaseFails_StillCancelsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            Status = OrderStatus.Approved
        };
        var reservations = new List<ReservedStock>
        {
            new ReservedStock(Guid.NewGuid())
            {
                OrderId = orderId,
                Status = ReservationStatus.Reserved
            }
        };
        var dto = new CancelOrderDto
        {
            OrderId = orderId,
            Reason = "Test"
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockReservedStockRepository.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(reservations);
        _mockServiceInvoker
            .Setup(s => s.InvokeAsync(
                ServiceNames.Inventory,
                It.IsAny<string>(),
                HttpMethod.Delete))
            .ThrowsAsync(new Exception("Release failed"));
        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);

        // Act
        await _orderService.CancelOrderAsync(dto);

        // Assert
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Cancelled
        )), Times.Once);
    }

    #endregion

    #region QueryOrdersAsync Tests

    [Fact]
    public async Task QueryOrdersAsync_WithValidSpecification_ReturnsPaginatedResult()
    {
        // Arrange
        var spec = new Mock<ISpecification<Order>>();
        var orders = new List<Order>
        {
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-001", Status = OrderStatus.Draft },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-002", Status = OrderStatus.Approved }
        };
        var paginatedResult = new PaginatedResult<Order>(orders, 1, 10, 2);

        _mockOrderRepository.Setup(r => r.QueryAsync(It.IsAny<ISpecification<Order>>())).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(o => new OrderDto(o.Id) { OrderNumber = o.OrderNumber, Status = o.Status.ToString() });

        // Act
        var result = await _orderService.QueryOrdersAsync(spec.Object);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        _mockOrderRepository.Verify(r => r.QueryAsync(spec.Object), Times.Once);
    }

    #endregion
}
