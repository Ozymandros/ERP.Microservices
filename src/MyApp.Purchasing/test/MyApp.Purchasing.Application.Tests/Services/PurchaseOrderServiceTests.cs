using System;
using System.Dynamic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Constants;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _mockPurchaseOrderRepository;
    private readonly Mock<IPurchaseOrderLineRepository> _mockLineRepository;
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PurchaseOrderService>> _mockLogger;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IServiceInvoker> _mockServiceInvoker;
    private readonly PurchaseOrderService _purchaseOrderService;

    public PurchaseOrderServiceTests()
    {
        _mockPurchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
        _mockLineRepository = new Mock<IPurchaseOrderLineRepository>();
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PurchaseOrderService>>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockServiceInvoker = new Mock<IServiceInvoker>();

        _purchaseOrderService = new PurchaseOrderService(
            _mockPurchaseOrderRepository.Object,
            _mockLineRepository.Object,
            _mockSupplierRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockEventPublisher.Object,
            _mockServiceInvoker.Object);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithExistingId_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new PurchaseOrder(orderId) { OrderNumber = "PO-001" };
        var expectedDto = new PurchaseOrderDto { OrderNumber = "PO-001" };

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(order)).Returns(expectedDto);

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PO-001", result.OrderNumber);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync((PurchaseOrder?)null);

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByIdAsync(orderId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllPurchaseOrdersAsync_ReturnsAllOrders()
    {
        // Arrange
        var orders = new List<PurchaseOrder>
        {
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-001" },
            new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-002" }
        };
        var dtos = new List<PurchaseOrderDto>
        {
            new PurchaseOrderDto { OrderNumber = "PO-001" },
            new PurchaseOrderDto { OrderNumber = "PO-002" }
        };

        _mockPurchaseOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<IEnumerable<PurchaseOrderDto>>(orders)).Returns(dtos);

        // Act
        var result = await _purchaseOrderService.GetAllPurchaseOrdersAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetPurchaseOrdersBySupplierAsync_ReturnsSupplierOrders()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var orders = new List<PurchaseOrder>
        {
            new PurchaseOrder(Guid.NewGuid()) { SupplierId = supplierId }
        };
        var dtos = new List<PurchaseOrderDto>
        {
            new PurchaseOrderDto { SupplierId = supplierId }
        };

        _mockPurchaseOrderRepository.Setup(r => r.GetBySuppliersIdAsync(supplierId)).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<IEnumerable<PurchaseOrderDto>>(orders)).Returns(dtos);

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(supplierId);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPurchaseOrdersByStatusAsync_ReturnsOrdersWithStatus()
    {
        // Arrange
        var status = PurchaseOrderStatus.Draft;
        var orders = new List<PurchaseOrder>
        {
            new PurchaseOrder(Guid.NewGuid()) { Status = status }
        };
        var dtos = new List<PurchaseOrderDto>
        {
            new PurchaseOrderDto()
        };

        _mockPurchaseOrderRepository.Setup(r => r.GetByStatusAsync(status)).ReturnsAsync(orders);
        _mockMapper.Setup(m => m.Map<IEnumerable<PurchaseOrderDto>>(orders)).Returns(dtos);

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(status);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithValidSupplier_CreatesOrder()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier(supplierId);
        var dto = new CreateUpdatePurchaseOrderDto
        {
            SupplierId = supplierId,
            Status = 0
        };
        var order = new PurchaseOrder(Guid.NewGuid())
        {
            SupplierId = supplierId,
            Lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine { Quantity = 10, UnitPrice = 5.00m }
            }
        };
        var createdOrder = new PurchaseOrder(Guid.NewGuid());
        var expectedDto = new PurchaseOrderDto();

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(supplier);
        _mockMapper.Setup(m => m.Map<PurchaseOrder>(dto)).Returns(order);
        _mockPurchaseOrderRepository.Setup(r => r.AddAsync(order)).ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(createdOrder)).Returns(expectedDto);

        // Act
        var result = await _purchaseOrderService.CreatePurchaseOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.00m, order.TotalAmount); // 10 * 5.00
        _mockPurchaseOrderRepository.Verify(r => r.AddAsync(order), Times.Once);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithNonExistentSupplier_ThrowsKeyNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var dto = new CreateUpdatePurchaseOrderDto { SupplierId = supplierId };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync((Supplier?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _purchaseOrderService.CreatePurchaseOrderAsync(dto));

        Assert.Contains("not found", exception.Message);
        _mockPurchaseOrderRepository.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithExistingOrder_UpdatesSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var order = new PurchaseOrder(orderId)
        {
            SupplierId = supplierId,
            Lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine { Quantity = 5, UnitPrice = 10.00m }
            }
        };
        var updateDto = new CreateUpdatePurchaseOrderDto
        {
            SupplierId = supplierId,
            Status = 1
        };
        var updatedOrder = new PurchaseOrder(Guid.NewGuid());
        var expectedDto = new PurchaseOrderDto();

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map(updateDto, order));
        _mockPurchaseOrderRepository.Setup(r => r.UpdateAsync(order)).ReturnsAsync(updatedOrder);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(updatedOrder)).Returns(expectedDto);

        // Act
        var result = await _purchaseOrderService.UpdatePurchaseOrderAsync(orderId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.00m, order.TotalAmount); // 5 * 10.00
        _mockPurchaseOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithNonExistentOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new CreateUpdatePurchaseOrderDto();

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync((PurchaseOrder?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _purchaseOrderService.UpdatePurchaseOrderAsync(orderId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockPurchaseOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithChangedSupplier_ValidatesNewSupplier()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var oldSupplierId = Guid.NewGuid();
        var newSupplierId = Guid.NewGuid();
        var newSupplier = new Supplier(newSupplierId);
        var order = new PurchaseOrder(orderId)
        {
            SupplierId = oldSupplierId,
            Lines = new List<PurchaseOrderLine>()
        };
        var updateDto = new CreateUpdatePurchaseOrderDto
        {
            SupplierId = newSupplierId,
            Status = 0
        };

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync(order);
        _mockSupplierRepository.Setup(r => r.GetByIdAsync(newSupplierId)).ReturnsAsync(newSupplier);
        _mockMapper.Setup(m => m.Map(updateDto, order));
        _mockPurchaseOrderRepository.Setup(r => r.UpdateAsync(order)).ReturnsAsync(order);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(order)).Returns(new PurchaseOrderDto());

        // Act
        await _purchaseOrderService.UpdatePurchaseOrderAsync(orderId, updateDto);

        // Assert
        _mockSupplierRepository.Verify(r => r.GetByIdAsync(newSupplierId), Times.Once);
    }

    [Fact]
    public async Task UpdatePurchaseOrderStatusAsync_WithExistingOrder_UpdatesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new PurchaseOrder(orderId) { Status = PurchaseOrderStatus.Draft };
        var newStatus = PurchaseOrderStatus.Approved;
        var updatedOrder = new PurchaseOrder(Guid.NewGuid()) { Status = newStatus };
        var expectedDto = new PurchaseOrderDto();

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync(order);
        _mockPurchaseOrderRepository.Setup(r => r.UpdateAsync(order)).ReturnsAsync(updatedOrder);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(updatedOrder)).Returns(expectedDto);

        // Act
        var result = await _purchaseOrderService.UpdatePurchaseOrderStatusAsync(orderId, newStatus);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newStatus, order.Status);
        _mockPurchaseOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task UpdatePurchaseOrderStatusAsync_WithNonExistentOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var status = PurchaseOrderStatus.Approved;

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(orderId)).ReturnsAsync((PurchaseOrder?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _purchaseOrderService.UpdatePurchaseOrderStatusAsync(orderId, status));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task DeletePurchaseOrderAsync_WithExistingOrder_DeletesOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new PurchaseOrder(orderId);

        _mockPurchaseOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        await _purchaseOrderService.DeletePurchaseOrderAsync(orderId);

        // Assert
        _mockPurchaseOrderRepository.Verify(r => r.DeleteAsync(order), Times.Once);
    }

    [Fact]
    public async Task DeletePurchaseOrderAsync_WithNonExistentOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockPurchaseOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((PurchaseOrder?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _purchaseOrderService.DeletePurchaseOrderAsync(orderId));

        Assert.Contains("not found", exception.Message);
        _mockPurchaseOrderRepository.Verify(r => r.DeleteAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    #region ReceivePurchaseOrderAsync Tests

    [Fact]
    public async Task ReceivePurchaseOrderAsync_WithValidPO_CreatesAndFulfillsInboundOrder()
    {
        // Arrange
        var poId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var poLineId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var po = new PurchaseOrder(poId)
        {
            OrderNumber = "PO-123",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Approved,
            Lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine { Id = poLineId, ProductId = productId, Quantity = 10, ReceivedQuantity = 0 }
            }
        };

        var dto = new ReceivePurchaseOrderDto
        {
            PurchaseOrderId = poId,
            WarehouseId = warehouseId,
            ReceivedDate = DateTime.UtcNow,
            Lines = new List<ReceivePurchaseOrderLineDto>
            {
                new ReceivePurchaseOrderLineDto { PurchaseOrderLineId = poLineId, ReceivedQuantity = 5 }
            }
        };

        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(poId)).ReturnsAsync(po);
        
        var fulfillmentOrderResponse = new OrderDto(Guid.NewGuid());

        _mockServiceInvoker.Setup(s => s.InvokeAsync<CreateUpdateOrderDto, OrderDto>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Post,
            It.IsAny<CreateUpdateOrderDto>(),
            default))
            .ReturnsAsync(fulfillmentOrderResponse);

        // Mock Order fulfillment
        _mockServiceInvoker.Setup(s => s.InvokeAsync<FulfillOrderDto, OrderDto>(
            ServiceNames.Orders,
            It.Is<string>(path => path.EndsWith("/fulfill")),
            HttpMethod.Post,
            It.IsAny<FulfillOrderDto>(),
            default))
            .ReturnsAsync(new OrderDto(Guid.NewGuid()));

        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(po)).Returns(new PurchaseOrderDto());

        // Act
        var result = await _purchaseOrderService.ReceivePurchaseOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, po.Lines.First().ReceivedQuantity);
        
        _mockServiceInvoker.Verify(s => s.InvokeAsync<CreateUpdateOrderDto, OrderDto>(ServiceNames.Orders, ApiEndpoints.Orders.Base, HttpMethod.Post, It.IsAny<CreateUpdateOrderDto>(), default), Times.Once);
        _mockServiceInvoker.Verify(s => s.InvokeAsync<FulfillOrderDto, OrderDto>(ServiceNames.Orders, It.Is<string>(path => path.EndsWith("/fulfill")), HttpMethod.Post, It.IsAny<FulfillOrderDto>(), default), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync(MessagingConstants.Topics.PurchasingLineReceived, It.IsAny<PurchaseOrderLineReceivedEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_WithNonExistentPO_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new ReceivePurchaseOrderDto { PurchaseOrderId = Guid.NewGuid() };
        _mockPurchaseOrderRepository.Setup(r => r.GetWithLinesAsync(It.IsAny<Guid>())).ReturnsAsync((PurchaseOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _purchaseOrderService.ReceivePurchaseOrderAsync(dto));
    }

    #endregion

    [Fact]
    public async Task CreatePurchaseOrderAsync_GeneratesOrderNumberServerSide()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier(supplierId);
        var dto = new CreateUpdatePurchaseOrderDto { SupplierId = supplierId, Status = 0 };
        var order = new PurchaseOrder(Guid.NewGuid()) { SupplierId = supplierId, Lines = new List<PurchaseOrderLine>() };
        var createdOrder = new PurchaseOrder(Guid.NewGuid()) { OrderNumber = "PO-TEST-12345" };
        var expectedDto = new PurchaseOrderDto { OrderNumber = "PO-TEST-12345" };

        _mockSupplierRepository.Setup(r => r.GetByIdAsync(supplierId)).ReturnsAsync(supplier);
        _mockMapper.Setup(m => m.Map<PurchaseOrder>(dto)).Returns(order);
        _mockPurchaseOrderRepository.Setup(r => r.AddAsync(order)).ReturnsAsync(createdOrder);
        _mockMapper.Setup(m => m.Map<PurchaseOrderDto>(createdOrder)).Returns(expectedDto);

        // Act
        var result = await _purchaseOrderService.CreatePurchaseOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.OrderNumber));
        Assert.StartsWith("PO-", result.OrderNumber);
        _mockPurchaseOrderRepository.Verify(r => r.AddAsync(It.Is<PurchaseOrder>(o => !string.IsNullOrWhiteSpace(o.OrderNumber))), Times.Once);
    }
}
