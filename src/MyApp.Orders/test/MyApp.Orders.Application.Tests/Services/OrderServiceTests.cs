using AutoMapper;
using Moq;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Services;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using Xunit;

namespace MyApp.Orders.Application.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IOrderLineRepository> _mockOrderLineRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockOrderLineRepository = new Mock<IOrderLineRepository>();
        _mockMapper = new Mock<IMapper>();

        _orderService = new OrderService(
            _mockOrderRepository.Object,
            _mockOrderLineRepository.Object,
            _mockMapper.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CallsAddAsyncOnRepository()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-001",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 5, 10.00m, 50.00m)
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-001",
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 5, UnitPrice = 10.00m }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid(), default, DateTime.UtcNow, "", null, null, "ORD-001", Guid.NewGuid(), "Draft", 50.00m, new List<OrderLineDto>()));

        // Act
        var result = await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => 
            o.Status == OrderStatus.Draft && 
            o.TotalAmount == 50.00m &&
            o.Id != Guid.Empty &&
            o.Lines.All(l => l.OrderId == o.Id && l.Id != Guid.Empty)
        )), Times.Once);
        
        _mockMapper.Verify(m => m.Map<Order>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<OrderDto>(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CalculatesTotalAmountFromLines()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-002",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 2, 15.50m, 31.00m),
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 3, 20.00m, 60.00m)
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            OrderNumber = "ORD-002",
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 15.50m },
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 3, UnitPrice = 20.00m }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid(), default, DateTime.UtcNow, "", null, null, "ORD-002", Guid.NewGuid(), "Draft", 91.00m, new List<OrderLineDto>()));

        // Act
        await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => 
            o.TotalAmount == 91.00m  // (2 * 15.50) + (3 * 20.00)
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsOrderStatusToDraft()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-003",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 1, 100.00m, 100.00m)
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100.00m }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid(), default, DateTime.UtcNow, "", null, null, "ORD-003", Guid.NewGuid(), "Draft", 100.00m, null));

        // Act
        await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => 
            o.Status == OrderStatus.Draft
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AssignsGuidsToOrderAndLines()
    {
        // Arrange
        var createDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-004",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 1, 10.00m, 10.00m),
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 2, 20.00m, 40.00m)
            }
        };

        var mappedOrder = new Order(Guid.NewGuid())
        {
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10.00m },
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 20.00m }
            }
        };

        _mockMapper.Setup(m => m.Map<Order>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns(new OrderDto(Guid.NewGuid(), default, DateTime.UtcNow, "", null, null, "ORD-004", Guid.NewGuid(), "Draft", 50.00m, null));

        // Act
        await _orderService.CreateAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => 
            o.Id != Guid.Empty &&
            o.Lines.All(l => l.Id != Guid.Empty && l.OrderId == o.Id)
        )), Times.Once);
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
            OrderNumber = "ORD-005",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Confirmed,
            TotalAmount = 250.00m
        };

        var expectedDto = new OrderDto(Guid.NewGuid(), default, DateTime.UtcNow, "", null, null, "ORD-005", Guid.NewGuid(), "Confirmed", 250.00m, null);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<OrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _orderService.GetByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORD-005", result.OrderNumber);
        Assert.Equal("Confirmed", result.Status);
        Assert.Equal(250.00m, result.TotalAmount);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockMapper.Verify(m => m.Map<OrderDto>(order), Times.Once);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsAllOrdersAsDtos()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-006", TotalAmount = 100.00m },
            new Order(Guid.NewGuid()) { OrderNumber = "ORD-007", TotalAmount = 200.00m }
        };

        _mockOrderRepository.Setup(r => r.ListAsync()).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns((Order o) => new OrderDto(o.Id, default, o.OrderDate, "", null, null, o.OrderNumber, o.CustomerId, "Draft", o.TotalAmount, null));

        // Act
        var result = await _orderService.ListAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, o => o.OrderNumber == "ORD-006");
        Assert.Contains(resultList, o => o.OrderNumber == "ORD-007");

        _mockOrderRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.ListAsync()).ReturnsAsync(new List<Order>());

        // Act
        var result = await _orderService.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockOrderRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingOrder_UpdatesSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order(orderId)
        {
            OrderNumber = "ORD-008-OLD",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Status = OrderStatus.Draft,
            TotalAmount = 100.00m,
            Lines = new List<OrderLine>()
        };

        var newCustomerId = Guid.NewGuid();
        var updateDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-008-NEW",
            CustomerId = newCustomerId,
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 3, 25.00m, 75.00m)
            }
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        // Act
        await _orderService.UpdateAsync(orderId, updateDto);

        // Assert
        Assert.Equal("ORD-008-NEW", existingOrder.OrderNumber);
        Assert.Equal(newCustomerId, existingOrder.CustomerId);
        Assert.Equal(75.00m, existingOrder.TotalAmount);
        Assert.Single(existingOrder.Lines);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrder_DoesNotCallUpdateAsync()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-009",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Lines = new List<OrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        await _orderService.UpdateAsync(orderId, updateDto);

        // Assert
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllLinesAndRecalculatesTotal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order(orderId)
        {
            OrderNumber = "ORD-010",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Draft,
            TotalAmount = 100.00m,
            Lines = new List<OrderLine>
            {
                new OrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 50.00m }
            }
        };

        var updateDto = new CreateUpdateOrderDto
        {
            OrderNumber = "ORD-010",
            CustomerId = existingOrder.CustomerId,
            OrderDate = existingOrder.OrderDate,
            Lines = new List<OrderLineDto>
            {
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 1, 30.00m, 30.00m),
                new OrderLineDto(Guid.NewGuid(), default, "", null, null, Guid.NewGuid(), 2, 20.00m, 40.00m)
            }
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        // Act
        await _orderService.UpdateAsync(orderId, updateDto);

        // Assert
        Assert.Equal(2, existingOrder.Lines.Count);
        Assert.Equal(70.00m, existingOrder.TotalAmount);
        Assert.All(existingOrder.Lines, line =>
        {
            Assert.Equal(orderId, line.OrderId);
            Assert.NotEqual(Guid.Empty, line.Id);
        });

        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
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
