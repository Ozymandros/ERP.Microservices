using System.Dynamic;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Ports;
using MyApp.Sales.Application.Internals;
using MyApp.Sales.Application.Services;
using MyApp.Sales.Application.UseCases.SalesOrders;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Sales.Application.Tests.Services;

/// <summary>Unit tests for <see cref="SalesOrderService"/>.</summary>
public class SalesOrderServiceTests
{
    private readonly Mock<ISalesOrderRepository> _mockOrderRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<SalesOrderService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IInventoryAvailabilityPort> _mockInventoryPort;
    private readonly Mock<IOrdersFulfillmentPort> _mockOrdersFulfillmentPort;
    private readonly SalesOrderService _salesOrderService;

    /// <summary>Initializes a new <see cref="SalesOrderServiceTests"/> with fresh mocks and a <see cref="SalesOrderService"/> instance.</summary>
    public SalesOrderServiceTests()
    {
        _mockOrderRepository = new Mock<ISalesOrderRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<SalesOrderService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockInventoryPort = new Mock<IInventoryAvailabilityPort>();
        _mockOrdersFulfillmentPort = new Mock<IOrdersFulfillmentPort>();
        _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        var boundary = new SalesTransactionBoundary(
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            NullLogger<SalesTransactionBoundary>.Instance);
        var checkStock = new CheckStockAvailabilityUseCase(
            _mockInventoryPort.Object,
            NullLogger<CheckStockAvailabilityUseCase>.Instance);
        var confirmQuote = new ConfirmQuoteUseCase(
            _mockOrderRepository.Object,
            _mockOrdersFulfillmentPort.Object,
            checkStock,
            _mockMapper.Object,
            boundary,
            NullLogger<ConfirmQuoteUseCase>.Instance);
        var createReturn = new CreateReturnUseCase(
            _mockOrderRepository.Object,
            _mockOrdersFulfillmentPort.Object,
            _mockMapper.Object,
            boundary,
            NullLogger<CreateReturnUseCase>.Instance);

        _salesOrderService = new SalesOrderService(
            _mockOrderRepository.Object,
            _mockCustomerRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object,
            checkStock,
            confirmQuote,
            createReturn);
    }

    private void MockProductAvailability(Guid productId, int totalAvailable)
    {
        _mockInventoryPort
            .Setup(p => p.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAvailabilitySnapshot(productId, totalAvailable, []));
    }

    private void MockAnyProductAvailability(int totalAvailable)
    {
        _mockInventoryPort
            .Setup(p => p.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid pid, CancellationToken _) => new ProductAvailabilitySnapshot(pid, totalAvailable, []));
    }

    #region GetSalesOrderByIdAsync Tests

    /// <summary>Verifies that GetSalesOrderByIdAsync returns the mapped SalesOrderDto when the order exists.</summary>
    [Fact]
    public async Task GetSalesOrderByIdAsync_WithExistingId_ReturnsSalesOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder(orderId) { OrderNumber = "SO-001" };
        var expectedDto = new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-001",
            CustomerId = Guid.NewGuid(),
            Status = 0,
            TotalAmount = 250.00m,
            Customer = null,
            Lines = new List<SalesOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.GetSalesOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-001", result.OrderNumber);
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
    }

    /// <summary>Verifies that GetSalesOrderByIdAsync returns null when no order exists with the given identifier.</summary>
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

    #region GetSalesOrderByOrderNumberAsync Tests

    /// <summary>Verifies that GetSalesOrderByOrderNumberAsync returns the mapped SalesOrderDto when the order number exists.</summary>
    [Fact]
    public async Task GetSalesOrderByOrderNumberAsync_WithExistingNumber_ReturnsSalesOrderDto()
    {
        // Arrange
        var orderNumber = "SO-001";
        var order = new SalesOrder(Guid.NewGuid()) { OrderNumber = orderNumber };
        var expectedDto = new SalesOrderDto(Guid.NewGuid())
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid()
        };

        _mockOrderRepository.Setup(r => r.GetByOrderNumberAsync(orderNumber)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.GetSalesOrderByOrderNumberAsync(orderNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderNumber, result.OrderNumber);
        _mockOrderRepository.Verify(r => r.GetByOrderNumberAsync(orderNumber), Times.Once);
    }

    /// <summary>Verifies that GetSalesOrderByOrderNumberAsync returns null when no order exists with the given order number.</summary>
    [Fact]
    public async Task GetSalesOrderByOrderNumberAsync_WithNonExistentNumber_ReturnsNull()
    {
        // Arrange
        var orderNumber = "NONEXISTENT";
        _mockOrderRepository.Setup(r => r.GetByOrderNumberAsync(orderNumber)).ReturnsAsync((SalesOrder?)null);

        // Act
        var result = await _salesOrderService.GetSalesOrderByOrderNumberAsync(orderNumber);

        // Assert
        Assert.Null(result);
        _mockOrderRepository.Verify(r => r.GetByOrderNumberAsync(orderNumber), Times.Once);
    }

    #endregion

    #region ListSalesOrdersAsync Tests

    /// <summary>Verifies that ListSalesOrdersAsync returns all sales orders mapped to their DTOs.</summary>
    [Fact]
    public async Task ListSalesOrdersAsync_ReturnsAllOrders()
    {
        // Arrange
        var orders = new List<SalesOrder>
        {
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-001" },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-002" }
        };

        var orderDtos = new List<SalesOrderDto>
        {
            new SalesOrderDto(Guid.NewGuid())
            {
                OrderDate = DateTime.UtcNow,
                OrderNumber = "SO-001",
                CustomerId = Guid.NewGuid(),
                Status = 0,
                TotalAmount = 100.00m,
                Customer = null!,
                Lines = new List<SalesOrderLineDto>()
            },
            new SalesOrderDto(Guid.NewGuid())
            {
                OrderDate = DateTime.UtcNow,
                OrderNumber = "SO-002",
                CustomerId = Guid.NewGuid(),
                Status = 0,
                TotalAmount = 200.00m,
                Customer = null!,
                Lines = new List<SalesOrderLineDto>()
            }
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

    /// <summary>Verifies that CreateSalesOrderAsync creates and persists a sales order when the specified customer exists.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_WithValidCustomer_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Test Customer" };

        var createDto = new CreateUpdateSalesOrderDto(customerId, DateTime.UtcNow, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(Guid.NewGuid(), 5, 10.00m)
        });

        var mappedOrder = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = "SO-003",
            CustomerId = customerId,
            Lines = new List<SalesOrderLine>()
        };

        var expectedDto = new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-003",
            CustomerId = customerId,
            Status = 0,
            TotalAmount = 50.00m,
            Customer = null!,
            Lines = new List<SalesOrderLineDto>()
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine(Guid.NewGuid())
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

    /// <summary>Verifies that CreateSalesOrderAsync throws KeyNotFoundException when the specified customer does not exist.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_WithNonExistentCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var createDto = new CreateUpdateSalesOrderDto(customerId, DateTime.UtcNow, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _salesOrderService.CreateSalesOrderAsync(createDto));

        Assert.Contains("not found", exception.Message);
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(customerId), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    /// <summary>Verifies that CreateSalesOrderAsync correctly computes TotalAmount as the sum of each line's Quantity multiplied by UnitPrice.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_CalculatesTotalFromLines()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId);

        var createDto = new CreateUpdateSalesOrderDto(customerId, DateTime.UtcNow, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(Guid.NewGuid(), 2, 15.50m),
            new CreateUpdateSalesOrderLineDto(Guid.NewGuid(), 3, 20.00m)
        });

        var mappedOrder = new SalesOrder(Guid.NewGuid()) { Lines = new List<SalesOrderLine>() };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine(Guid.NewGuid())
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice
            });
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-001",
            CustomerId = customerId,
            Status = 0,
            TotalAmount = 91.00m,
            Customer = null!,
            Lines = new List<SalesOrderLineDto>()
        });

        // Act
        await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o =>
            o.TotalAmount == 91.00m  // (2 * 15.50) + (3 * 20.00)
        )), Times.Once);
    }

    /// <summary>Verifies that CreateSalesOrderAsync sets OrderDate to the current UTC time when a default date is supplied.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_UsesUtcNowIfOrderDateIsDefault()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId);

        var createDto = new CreateUpdateSalesOrderDto(customerId, default, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());

        var mappedOrder = new SalesOrder(Guid.NewGuid()) { Lines = new List<SalesOrderLine>() };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-002",
            CustomerId = customerId,
            Status = 0,
            TotalAmount = 0,
            Customer = null,
            Lines = new List<SalesOrderLineDto>()
        });

        // Act
        await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o =>
            o.OrderDate != default &&
            (DateTime.UtcNow - o.OrderDate).TotalSeconds < 5  // Created within last 5 seconds
        )), Times.Once);
    }

    /// <summary>Verifies that CreateSalesOrderAsync generates a non-empty order number server-side with the expected "SO-" prefix.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_GeneratesOrderNumberServerSide()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Test Customer" };
        var createDto = new CreateUpdateSalesOrderDto(customerId, DateTime.UtcNow, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());
        var mappedOrder = new SalesOrder(Guid.NewGuid()) { CustomerId = customerId, Lines = new List<SalesOrderLine>() };
        var expectedDto = new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-TEST-12345",
            CustomerId = customerId,
            Status = 0,
            TotalAmount = 0,
            Customer = null,
            Lines = new List<SalesOrderLineDto>()
        };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(createDto)).Returns(mappedOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(expectedDto);
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<SalesOrder>())).ReturnsAsync(mappedOrder);

        // Act
        var result = await _salesOrderService.CreateSalesOrderAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.OrderNumber));
        Assert.StartsWith("SO-", result.OrderNumber);
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(o => !string.IsNullOrWhiteSpace(o.OrderNumber))), Times.Once);
    }

    #endregion

    #region UpdateSalesOrderAsync Tests

    /// <summary>Verifies that UpdateSalesOrderAsync applies the update DTO to an existing order and persists the changes.</summary>
    [Fact]
    public async Task UpdateSalesOrderAsync_WithExistingOrder_UpdatesSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingCustomerId = Guid.NewGuid();
        var existingOrder = new SalesOrder(orderId)
        {
            OrderNumber = "SO-OLD",
            CustomerId = existingCustomerId,
            Status = SalesOrderStatus.Draft,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto(existingCustomerId, DateTime.UtcNow, null, 1, 0, new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(Guid.NewGuid(), 3, 25.00m)
        });

        var expectedDto = new SalesOrderDto(Guid.NewGuid())
        {
            OrderDate = DateTime.UtcNow,
            OrderNumber = "SO-NEW",
            CustomerId = existingCustomerId,
            Status = 1,
            TotalAmount = 75.00m,
            Customer = null,
            Lines = new List<SalesOrderLineDto>()
        };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>()))
            .Returns((CreateUpdateSalesOrderLineDto dto) => new SalesOrderLine(Guid.NewGuid())
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice
            });
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-NEW", result.OrderNumber);
        Assert.Equal(SalesOrderStatus.Confirmed, existingOrder.Status);
        Assert.Equal(75.00m, existingOrder.TotalAmount);
        Assert.Single(existingOrder.Lines);

        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
    }

    /// <summary>Verifies that UpdateSalesOrderAsync throws InvalidOperationException when no order exists with the given identifier.</summary>
    [Fact]
    public async Task UpdateSalesOrderAsync_WithNonExistentOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new CreateUpdateSalesOrderDto(Guid.NewGuid(), DateTime.UtcNow, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((SalesOrder?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    /// <summary>Verifies that UpdateSalesOrderAsync validates the new customer against the repository when the CustomerId changes.</summary>
    [Fact]
    public async Task UpdateSalesOrderAsync_WithChangedCustomer_ValidatesNewCustomer()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var oldCustomerId = Guid.NewGuid();
        var newCustomerId = Guid.NewGuid();
        var newCustomer = new Customer(newCustomerId);

        var existingOrder = new SalesOrder(orderId)
        {
            CustomerId = oldCustomerId,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto(newCustomerId, existingOrder.OrderDate, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockCustomerRepository.Setup(r => r.GetByIdAsync(newCustomerId)).ReturnsAsync(newCustomer);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>())).Returns(new SalesOrderDto(Guid.NewGuid()));

        // Act
        await _salesOrderService.UpdateSalesOrderAsync(orderId, updateDto);

        // Assert
        _mockCustomerRepository.Verify(r => r.GetByIdAsync(newCustomerId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
    }

    /// <summary>Verifies that UpdateSalesOrderAsync throws InvalidOperationException when the new CustomerId does not exist.</summary>
    [Fact]
    public async Task UpdateSalesOrderAsync_WithChangedCustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var oldCustomerId = Guid.NewGuid();
        var newCustomerId = Guid.NewGuid();

        var existingOrder = new SalesOrder(orderId)
        {
            CustomerId = oldCustomerId,
            Lines = new List<SalesOrderLine>()
        };

        var updateDto = new CreateUpdateSalesOrderDto(newCustomerId, existingOrder.OrderDate, null, 0, 0, new List<CreateUpdateSalesOrderLineDto>());

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

    /// <summary>Verifies that DeleteSalesOrderAsync delegates to the repository's DeleteAsync with the correct order identifier.</summary>
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

    #region ListSalesOrdersPaginatedAsync Tests

    /// <summary>Verifies that ListSalesOrdersPaginatedAsync returns a correctly populated paginated result with the requested page and page size.</summary>
    [Fact]
    public async Task ListSalesOrdersPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var orders = new List<SalesOrder>
        {
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-001" },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-002" }
        };
        var paginatedResult = new PaginatedResult<SalesOrder>(orders, 1, 10, 2);
        var orderDtos = orders.Select(o => new SalesOrderDto(o.Id) { OrderNumber = o.OrderNumber }).ToList();

        _mockOrderRepository.Setup(r => r.GetAllPaginatedAsync(1, 10)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<IEnumerable<SalesOrderDto>>(orders)).Returns(orderDtos);

        // Act
        var result = await _salesOrderService.ListSalesOrdersPaginatedAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        _mockOrderRepository.Verify(r => r.GetAllPaginatedAsync(1, 10), Times.Once);
    }

    #endregion

    #region QuerySalesOrdersAsync Tests

    /// <summary>Verifies that QuerySalesOrdersAsync passes the specification to the repository and returns the mapped paginated result.</summary>
    [Fact]
    public async Task QuerySalesOrdersAsync_WithValidSpecification_ReturnsPaginatedResult()
    {
        // Arrange
        var spec = new Mock<ISpecification<SalesOrder>>();
        var orders = new List<SalesOrder>
        {
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-001", Status = SalesOrderStatus.Draft },
            new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-002", Status = SalesOrderStatus.Confirmed }
        };
        var paginatedResult = new PaginatedResult<SalesOrder>(orders, 1, 10, 2);
        var orderDtos = orders.Select(o => new SalesOrderDto(o.Id) { OrderNumber = o.OrderNumber, Status = (int)o.Status }).ToList();

        _mockOrderRepository.Setup(r => r.QueryAsync(It.IsAny<ISpecification<SalesOrder>>())).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>()))
            .Returns<SalesOrder>(o => orderDtos.First(d => d.Id == o.Id));

        // Act
        var result = await _salesOrderService.QuerySalesOrdersAsync(spec.Object);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        _mockOrderRepository.Verify(r => r.QueryAsync(spec.Object), Times.Once);
    }

    #endregion

    #region CreateQuoteAsync Tests

    /// <summary>Verifies that CreateQuoteAsync creates a draft quote with correct totals, expiry date, and publishes the SalesOrderCreated event.</summary>
    [Fact]
    public async Task CreateQuoteAsync_WithValidDto_CreatesQuote()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Test Customer" };
        var productId = Guid.NewGuid();
        var dto = new CreateQuoteDto(
            OrderNumber: "Q-001",
            CustomerId: customerId,
            OrderDate: DateTime.UtcNow,
            ValidityDays: 30,
            Lines: new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto(productId, 5, 10.00m)
            }
        );

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        MockProductAvailability(productId, 10);
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<SalesOrder>())).ReturnsAsync((SalesOrder so) => so);
        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), default)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>()))
            .Returns((SalesOrder so) => new SalesOrderDto(so.Id) { OrderNumber = so.OrderNumber, IsQuote = so.IsQuote });

        // Act
        var result = await _salesOrderService.CreateQuoteAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OrderNumber.Should().Be("Q-001");
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<SalesOrder>(so =>
            so.IsQuote == true &&
            so.QuoteExpiryDate.HasValue &&
            so.Status == SalesOrderStatus.Draft &&
            so.TotalAmount == 50.00m &&
            so.Lines.Count == 1
        )), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync(MessagingConstants.Topics.SalesOrderCreated, It.IsAny<SalesOrderCreatedEvent>(), default), Times.Once);
    }

    /// <summary>Verifies that CreateQuoteAsync throws InvalidOperationException when the specified customer does not exist.</summary>
    [Fact]
    public async Task CreateQuoteAsync_WithNonExistentCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var dto = new CreateQuoteDto(
            OrderNumber: "Q-002",
            CustomerId: customerId,
            OrderDate: DateTime.UtcNow,
            ValidityDays: 30,
            Lines: new List<CreateUpdateSalesOrderLineDto>()
        );

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await _salesOrderService.CreateQuoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Customer with ID {customerId} not found*");
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<SalesOrder>()), Times.Never);
    }

    /// <summary>Verifies that CreateQuoteAsync still creates the quote and persists it even when stock is insufficient, rather than blocking the operation.</summary>
    [Fact]
    public async Task CreateQuoteAsync_WithInsufficientStock_StillCreatesQuoteButLogsWarning()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Test Customer" };
        var productId = Guid.NewGuid();
        var dto = new CreateQuoteDto(
            OrderNumber: "Q-003",
            CustomerId: customerId,
            OrderDate: DateTime.UtcNow,
            ValidityDays: 30,
            Lines: new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto(productId, 100, 10.00m)
            }
        );

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        MockProductAvailability(productId, 5);
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<SalesOrder>())).ReturnsAsync((SalesOrder so) => so);
        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), default)).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(It.IsAny<SalesOrder>()))
            .Returns((SalesOrder so) => new SalesOrderDto(so.Id) { OrderNumber = so.OrderNumber });

        // Act
        var result = await _salesOrderService.CreateQuoteAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<SalesOrder>()), Times.Once);
    }

    #endregion

    #region ConfirmQuoteAsync Tests - Additional Scenarios

    /// <summary>Verifies that ConfirmQuoteAsync transitions the quote to Confirmed status, links the fulfillment order, and publishes the SalesOrderConfirmed event.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithValidQuote_CreatesFulfillmentOrderAndUpdatesStatus()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var quote = new SalesOrder(quoteId)
        {
            OrderNumber = "Q-123",
            IsQuote = true,
            Status = SalesOrderStatus.Draft,
            CustomerId = customerId,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(7),
            Lines = new List<SalesOrderLine>
            {
                new SalesOrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 100 }
            }
        };

        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = warehouseId, ShippingAddress = "123 Street" };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync(quote);

        MockAnyProductAvailability(10);
        var fulfillmentOrderId = Guid.NewGuid();
        _mockOrdersFulfillmentPort
            .Setup(p => p.CreateWithReservationAsync(It.IsAny<CreateFulfillmentOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalOrderSnapshot(fulfillmentOrderId, "ORD-001"));

        _mockMapper.Setup(m => m.Map<SalesOrderDto>(quote)).Returns(new SalesOrderDto(quoteId));

        // Act
        var result = await _salesOrderService.ConfirmQuoteAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SalesOrderStatus.Confirmed, quote.Status);
        Assert.NotNull(quote.ConvertedToOrderId);

        _mockOrderRepository.Verify(r => r.UpdateAsync(quote), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync(MessagingConstants.Topics.SalesOrderConfirmed, It.IsAny<SalesOrderConfirmedEvent>(), default), Times.Once);
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when the quote's expiry date has passed.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithExpiredQuote_ThrowsInvalidOperationException()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new SalesOrder(quoteId)
        {
            IsQuote = true,
            Status = SalesOrderStatus.Draft,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(-1)
        };
        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = Guid.NewGuid(), ShippingAddress = null };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync(quote);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _salesOrderService.ConfirmQuoteAsync(dto));
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when there is insufficient stock to fulfil the quote lines.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new SalesOrder(quoteId)
        {
            IsQuote = true,
            Status = SalesOrderStatus.Draft,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(7),
            Lines = new List<SalesOrderLine>
            {
                new SalesOrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 100 }
            }
        };
        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = Guid.NewGuid(), ShippingAddress = null };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync(quote);

        MockAnyProductAvailability(0);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _salesOrderService.ConfirmQuoteAsync(dto));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when no quote exists with the given identifier.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithNonExistentQuote_ThrowsInvalidOperationException()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = Guid.NewGuid() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync((SalesOrder?)null);

        // Act
        Func<Task> act = async () => await _salesOrderService.ConfirmQuoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Quote {quoteId} not found*");
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when the found record is a regular order rather than a quote.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithNonQuoteOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder(orderId)
        {
            IsQuote = false,
            Status = SalesOrderStatus.Draft
        };
        var dto = new ConfirmQuoteDto { QuoteId = orderId, WarehouseId = Guid.NewGuid() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _salesOrderService.ConfirmQuoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*is not a quote*");
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when the quote is not in Draft status.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithNonDraftStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new SalesOrder(quoteId)
        {
            IsQuote = true,
            Status = SalesOrderStatus.Confirmed,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(7)
        };
        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = Guid.NewGuid() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync(quote);

        // Act
        Func<Task> act = async () => await _salesOrderService.ConfirmQuoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*cannot be confirmed*");
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws InvalidOperationException when the downstream Orders service call fails to create the fulfilment order.</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WhenFulfillmentOrderCreationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new SalesOrder(quoteId)
        {
            IsQuote = true,
            Status = SalesOrderStatus.Draft,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(7),
            Lines = new List<SalesOrderLine>
            {
                new SalesOrderLine(Guid.NewGuid()) { ProductId = Guid.NewGuid(), Quantity = 2 }
            }
        };
        var dto = new ConfirmQuoteDto { QuoteId = quoteId, WarehouseId = Guid.NewGuid() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(quoteId)).ReturnsAsync(quote);
        MockAnyProductAvailability(10);
        _mockOrdersFulfillmentPort
            .Setup(p => p.CreateWithReservationAsync(It.IsAny<CreateFulfillmentOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Order creation failed"));

        // Act
        Func<Task> act = async () => await _salesOrderService.ConfirmQuoteAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Failed to create fulfillment order*");
    }

    #endregion

    #region CheckStockAvailabilityAsync Tests

    /// <summary>Verifies that CheckStockAvailabilityAsync returns an available status when the inventory service reports sufficient stock.</summary>
    [Fact]
    public async Task CheckStockAvailabilityAsync_WithAvailableStock_ReturnsAvailableStatus()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var lines = new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(productId, 5, 10.00m)
        };

        var warehouseId = Guid.NewGuid();
        _mockInventoryPort
            .Setup(p => p.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductAvailabilitySnapshot(
                productId,
                10,
                [new WarehouseAvailabilitySnapshot(warehouseId, "Main", 10)]));

        // Act
        var result = await _salesOrderService.CheckStockAvailabilityAsync(lines);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productId);
        result[0].RequestedQuantity.Should().Be(5);
        result[0].AvailableQuantity.Should().Be(10);
        result[0].IsAvailable.Should().BeTrue();
        result[0].WarehouseStock.Should().HaveCount(1);
    }

    /// <summary>Verifies that CheckStockAvailabilityAsync returns an unavailable status when the inventory service reports less stock than requested.</summary>
    [Fact]
    public async Task CheckStockAvailabilityAsync_WithInsufficientStock_ReturnsUnavailableStatus()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var lines = new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(productId, 10, 10.00m)
        };

        MockProductAvailability(productId, 5);

        // Act
        var result = await _salesOrderService.CheckStockAvailabilityAsync(lines);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].IsAvailable.Should().BeFalse();
        result[0].AvailableQuantity.Should().Be(5);
        result[0].RequestedQuantity.Should().Be(10);
    }

    /// <summary>Verifies that CheckStockAvailabilityAsync queries the inventory service for each product line and returns individual availability statuses.</summary>
    [Fact]
    public async Task CheckStockAvailabilityAsync_WithMultipleProducts_ChecksAllProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var lines = new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(productId1, 5, 10.00m),
            new CreateUpdateSalesOrderLineDto(productId2, 3, 20.00m)
        };

        MockProductAvailability(productId1, 10);
        MockProductAvailability(productId2, 2);

        // Act
        var result = await _salesOrderService.CheckStockAvailabilityAsync(lines);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].ProductId.Should().Be(productId1);
        result[0].IsAvailable.Should().BeTrue();
        result[1].ProductId.Should().Be(productId2);
        result[1].IsAvailable.Should().BeFalse();
    }

    /// <summary>Verifies that CheckStockAvailabilityAsync returns an unavailable status with zero quantity when the inventory service call throws an exception.</summary>
    [Fact]
    public async Task CheckStockAvailabilityAsync_WhenStockCheckFails_ReturnsUnavailable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var lines = new List<CreateUpdateSalesOrderLineDto>
        {
            new CreateUpdateSalesOrderLineDto(productId, 5, 10.00m)
        };

        _mockInventoryPort
            .Setup(p => p.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        var result = await _salesOrderService.CheckStockAvailabilityAsync(lines);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productId);
        result[0].IsAvailable.Should().BeFalse();
        result[0].AvailableQuantity.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Boundary Values

    /// <summary>Verifies that CreateSalesOrderAsync handles a line with the maximum decimal unit price without overflow.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_WithMaximumTotalAmount_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Customer" };
        var productId = Guid.NewGuid();
        var dto = new CreateUpdateSalesOrderDto(
            CustomerId: customerId,
            OrderDate: DateTime.UtcNow,
            ExpectedDeliveryDate: null,
            Status: 0,
            TotalAmount: 0,
            Lines: new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto(productId, 1, decimal.MaxValue / 2)
            }
        );

        var order = new SalesOrder(Guid.NewGuid()) { CustomerId = customerId, OrderDate = dto.OrderDate, Lines = new List<SalesOrderLine>() };
        var createdOrder = new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-GENERATED", CustomerId = customerId };
        var expectedDto = new SalesOrderDto(createdOrder.Id) { OrderNumber = createdOrder.OrderNumber };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(dto)).Returns(order);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>())).Returns((CreateUpdateSalesOrderLineDto ldto) => new SalesOrderLine(Guid.NewGuid()) { ProductId = ldto.ProductId, Quantity = ldto.Quantity, UnitPrice = ldto.UnitPrice });
        _mockOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SalesOrder>()); // For GenerateOrderNumberAsync
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<SalesOrder>())).ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(createdOrder)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.CreateSalesOrderAsync(dto);

        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>Verifies that CreateSalesOrderAsync creates a valid order even when all line quantities and unit prices are zero.</summary>
    [Fact]
    public async Task CreateSalesOrderAsync_WithZeroTotalAmount_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId) { Name = "Customer" };
        var dto = new CreateUpdateSalesOrderDto(
            CustomerId: customerId,
            OrderDate: DateTime.UtcNow,
            ExpectedDeliveryDate: null,
            Status: 0,
            TotalAmount: 0,
            Lines: new List<CreateUpdateSalesOrderLineDto>
            {
                new CreateUpdateSalesOrderLineDto(Guid.NewGuid(), 0, 0m)
            }
        );

        var order = new SalesOrder(Guid.NewGuid()) { CustomerId = customerId, OrderDate = dto.OrderDate, Lines = new List<SalesOrderLine>() };
        var createdOrder = new SalesOrder(Guid.NewGuid()) { OrderNumber = "SO-GENERATED", CustomerId = customerId };
        var expectedDto = new SalesOrderDto(createdOrder.Id) { OrderNumber = createdOrder.OrderNumber };

        _mockCustomerRepository.Setup(r => r.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<SalesOrder>(dto)).Returns(order);
        _mockMapper.Setup(m => m.Map<SalesOrderLine>(It.IsAny<CreateUpdateSalesOrderLineDto>())).Returns((CreateUpdateSalesOrderLineDto ldto) => new SalesOrderLine(Guid.NewGuid()) { ProductId = ldto.ProductId, Quantity = ldto.Quantity, UnitPrice = ldto.UnitPrice });
        _mockOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SalesOrder>()); // For GenerateOrderNumberAsync
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<SalesOrder>())).ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<SalesOrderDto>(createdOrder)).Returns(expectedDto);

        // Act
        var result = await _salesOrderService.CreateSalesOrderAsync(dto);

        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>Verifies that ConfirmQuoteAsync throws an exception for an expired quote (edge-case duplicate covering boundary behaviour).</summary>
    [Fact]
    public async Task ConfirmQuoteAsync_WithExpiredQuote_ThrowsException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder(orderId)
        {
            IsQuote = true,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(-1) // Expired
        };
        var dto = new ConfirmQuoteDto { QuoteId = orderId };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _salesOrderService.ConfirmQuoteAsync(dto));
    }

    [Fact]
    public async Task MarkShippedFromFulfillmentAsync_OutboundWithExternalOrderId_SetsShippedStatus()
    {
        var salesOrderId = Guid.NewGuid();
        var order = new SalesOrder(salesOrderId)
        {
            OrderNumber = "SO-100",
            Status = SalesOrderStatus.Confirmed
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(salesOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _salesOrderService.MarkShippedFromFulfillmentAsync(
            new OrderFulfilledEvent(
                Guid.NewGuid(),
                "ORD-1",
                OrderTypes.Outbound,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null,
                salesOrderId,
                []),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.SalesOrderId.Should().Be(salesOrderId);
        order.Status.Should().Be(SalesOrderStatus.Shipped);
        _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task MarkCancelledFromOrderAsync_ConfirmedOrder_SetsCancelled()
    {
        var salesOrderId = Guid.NewGuid();
        var order = new SalesOrder(salesOrderId)
        {
            OrderNumber = "SO-200",
            Status = SalesOrderStatus.Confirmed
        };
        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(salesOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _salesOrderService.MarkCancelledFromOrderAsync(
            new OrderCancelledEvent(Guid.NewGuid(), "customer request", salesOrderId),
            CancellationToken.None);

        result.Should().NotBeNull();
        order.Status.Should().Be(SalesOrderStatus.Cancelled);
    }

    [Fact]
    public async Task MarkInvoicedFromBillingAsync_ShippedOrder_SetsInvoiced()
    {
        var salesOrderId = Guid.NewGuid();
        var order = new SalesOrder(salesOrderId)
        {
            OrderNumber = "SO-300",
            Status = SalesOrderStatus.Shipped
        };
        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(salesOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _salesOrderService.MarkInvoicedFromBillingAsync(
            new InvoiceIssuedEvent(
                Guid.NewGuid(),
                "INV-1",
                Guid.NewGuid(),
                salesOrderId,
                10m,
                2m,
                12m,
                DateTime.UtcNow.AddDays(30)),
            CancellationToken.None);

        result.Should().NotBeNull();
        order.Status.Should().Be(SalesOrderStatus.Invoiced);
    }

    [Fact]
    public async Task CreateReturnAsync_ShippedOrder_CreatesReturnOrder()
    {
        var salesOrderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var order = new SalesOrder(salesOrderId)
        {
            OrderNumber = "SO-400",
            Status = SalesOrderStatus.Shipped,
            Lines =
            [
                new SalesOrderLine(Guid.NewGuid())
                {
                    SalesOrderId = salesOrderId,
                    ProductId = productId,
                    Quantity = 5,
                    UnitPrice = 10m
                }
            ]
        };
        var returnOrderId = Guid.NewGuid();

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(salesOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _mockOrdersFulfillmentPort
            .Setup(p => p.CreateOrderAsync(It.IsAny<CreateOperationalOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationalOrderSnapshot(returnOrderId, "SO-400-RET"));
        _mockMapper
            .Setup(m => m.Map<SalesOrderDto>(order))
            .Returns(new SalesOrderDto(salesOrderId) { OrderNumber = "SO-400", ReturnOrderId = returnOrderId });

        var result = await _salesOrderService.CreateReturnAsync(
            salesOrderId,
            new CreateSalesReturnDto
            {
                WarehouseId = warehouseId,
                Lines = [new CreateSalesReturnLineDto(productId, 2)]
            });

        result.ReturnOrderId.Should().Be(returnOrderId);
        order.ReturnOrderId.Should().Be(returnOrderId);
    }

    #endregion
}
