using AutoMapper;
using Moq;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Services;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using Xunit;

namespace MyApp.Sales.Application.Tests.Services;

public class SalesOrderServiceTests
{
    private readonly Mock<ISalesOrderRepository> _mockOrderRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SalesOrderService _salesOrderService;

    public SalesOrderServiceTests()
    {
        _mockOrderRepository = new Mock<ISalesOrderRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockMapper = new Mock<IMapper>();

        _salesOrderService = new SalesOrderService(
            _mockOrderRepository.Object,
            _mockCustomerRepository.Object,
            _mockMapper.Object);
    }

    #region GetSalesOrderByIdAsync Tests

    [Fact]
    public async Task GetSalesOrderByIdAsync_WithExistingId_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "SO-001",
            TotalAmount = 250.00m
        };

        var expectedDto = new SalesOrderDto
        {
            OrderNumber = "SO-001",
            TotalAmount = 250.00m
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.GetSalesOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-001", result.OrderNumber);
        Assert.Equal(250.00m, result.TotalAmount);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockMapper.Verify(m => m.Map<SalesOrderDto>(order), Times.Once);
    }

    [Fact]
    public async Task GetSalesOrderByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((SalesOrder?)null);

        // Act
        var result = await _salesOrderService.GetSalesOrderByIdAsync(orderId);

        // Assert
        Assert.Null(result);
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockMapper.Verify(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>()), Times.Never);
    }

    #endregion

    #region ListSalesOrdersAsync Tests

    [Fact]
    public async Task ListSalesOrdersAsync_ReturnsAllOrders()
    {
        // Arrange
        var orders = new List<SalesOrder>
        {
            new SalesOrder { Id = Guid.NewGuid(), OrderNumber = "SO-001", TotalAmount = 100.00m },
            new SalesOrder { Id = Guid.NewGuid(), OrderNumber = "SO-002", TotalAmount = 200.00m }
        };

        var orderDtos = new List<SalesOrderDto>
        {
            new SalesOrderDto { OrderNumber = "SO-001", TotalAmount = 100.00m },
            new SalesOrderDto { OrderNumber = "SO-002", TotalAmount = 200.00m }
        };

        _mockOrderRepository.Setup(r => r.ListAsync()).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<IEnumerable<SalesOrderDto>>(orders)).Returns(orderDtos);

        // Act
        var result = await _salesOrderService.ListSalesOrdersAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, o => o.OrderNumber == "SO-001");
        Assert.Contains(resultList, o => o.OrderNumber == "SO-002");

        _mockOrderRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    #endregion

    #region CreateSalesOrderAsync Tests

    [Fact]
    public async Task CreateSalesOrderAsync_WithValidCustomer_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer { Id = customerId, Name = "Test Customer" };

        var createDto = new CreateUpdateSalesOrderDto
        {
            OrderNumber = "SO-003",
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 5, UnitPrice = 10.00m }
            }
        };

        var mappedOrder = new SalesOrder
        {
            OrderNumber = "SO-003",
            CustomerId = customerId,
            Lines = new List<SalesOrderLine>()
        };

        var expectedDto = new SalesOrderDto { OrderNumber = "SO-003" };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice
            });
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-003", result.OrderNumber);

        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o =>
            o.Id != Guid.Empty &&
            o.TotalAmount == 50.00m &&
            o.Lines.Count == 1 &&
            o.Lines.All(l => l.Id != Guid.Empty && l.SalesOrderId == o.Id)
        )), Times.Once);
    }

    [Fact]
    public async Task CreateSalesOrderAsync_WithNonExistentCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var createDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = customerId,
            Lines = new List<CreateUpdateSalesOrderLineDto>()
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.CreateSalesOrderAsync(createDto));

        Assert.Contains("not found", exception.Message);
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    [Fact]
    public async Task CreateSalesOrderAsync_CalculatesTotalFromLines()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };

        var createDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Lines = new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 15.50m },
                new CreateUpdateSalesOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 3, UnitPrice = 20.00m }
            }
        };

        var mappedOrder = new SalesOrder { Lines = new List<SalesOrderLine>() };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice
            });
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto());

        // Act
        await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o =>
            o.TotalAmount == 91.00m  // (2 * 15.50) + (3 * 20.00)
        )), Times.Once);
    }

    [Fact]
    public async Task CreateSalesOrderAsync_UsesUtcNowIfOrderDateIsDefault()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer { Id = customerId };

        var createDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = customerId,
            OrderDate = default,
            Lines = new List<CreateUpdateSalesOrderLineDto>()
        };

        var mappedOrder = new SalesOrder { Lines = new List<SalesOrderLine>() };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto());

        // Act
        await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o =>
            o.OrderDate != default && 
            (DateTime.UtcNow - o.OrderDate).TotalSeconds < 5  // Created within last 5 seconds
        )), Times.Once);
    }

    #endregion

    #region UpdateSalesOrderAsync Tests

    [Fact]
    public async Task UpdateSalesOrderAsync_WithExistingOrder_UpdatesSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingCustomerId = Guid.NewGuid();
        var existingOrder = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "SO-OLD",
            CustomerId = existingCustomerId,
            Status = SalesOrderStatus.Draft,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto
        {
            OrderNumber = "SO-NEW",
            CustomerId = existingCustomerId,
            Status = 1,  // Confirmed
            Lines = new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto { ProductId = Guid.NewGuid(), Quantity = 3, UnitPrice = 25.00m }
            }
        };

        var expectedDto = new SalesOrderDto { OrderNumber = "SO-NEW" };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice
            });
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(existingOrder)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-NEW", existingOrder.OrderNumber);
        Assert.Equal(SalesOrderStatus.Confirmed, existingOrder.Status);
        Assert.Equal(75.00m, existingOrder.TotalAmount);
        Assert.Single(existingOrder.Lines);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
    }

    [Fact]
    public async Task UpdateSalesOrderAsync_WithNonExistentOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = Guid.NewGuid(),
            Lines = new List<CreateUpdateSalesOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((SalesOrder?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSalesOrderAsync_WithChangedCustomer_ValidatesNewCustomer()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var oldCustomerId = Guid.NewGuid();
        var newCustomerId = Guid.NewGuid();
        var newCustomer = new Customer { Id = newCustomerId };

        var existingOrder = new SalesOrder
        {
            Id = orderId,
            CustomerId = oldCustomerId,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = newCustomerId,
            Lines = new List<CreateUpdateSalesOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockCustomerRepository.Setup(r => r.GetByIdAsync(newCustomerId)).ReturnsAsync(newCustomer);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto());

        // Act
        await _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto);

        // Assert
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(newCustomerId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
    }

    [Fact]
    public async Task UpdateSalesOrderAsync_WithChangedCustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var oldCustomerId = Guid.NewGuid();
        var newCustomerId = Guid.NewGuid();

        var existingOrder = new SalesOrder
        {
            Id = orderId,
            CustomerId = oldCustomerId,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto
        {
            CustomerId = newCustomerId,
            Lines = new List<CreateUpdateSalesOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockCustomerRepository.Setup(r => r.GetByIdAsync(newCustomerId)).ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto));

        Assert.Contains("Customer", exception.Message);
        Assert.Contains("not found", exception.Message);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    #endregion

    #region DeleteSalesOrderAsync Tests

    [Fact]
    public async Task DeleteSalesOrderAsync_CallsRepositoryDeleteWithCorrectId()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        await _salesOrderService.DeleteSalesOrderAsync(orderId);

        // Assert
        _mockOrderRepository.Verify(r => r.DeleteAsync(orderId), Times.Once);
    }

    #endregion
}
