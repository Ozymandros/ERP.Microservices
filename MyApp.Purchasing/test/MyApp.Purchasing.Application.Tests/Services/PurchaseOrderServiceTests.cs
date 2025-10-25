using AutoMapper;
using Moq;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _mockPurchaseOrderRepository;
    private readonly Mock<IPurchaseOrderLineRepository> _mockLineRepository;
    private readonly Mock<ISupplierRepository> _mockSupplierRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PurchaseOrderService _purchaseOrderService;

    public PurchaseOrderServiceTests()
    {
        _mockPurchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
        _mockLineRepository = new Mock<IPurchaseOrderLineRepository>();
        _mockSupplierRepository = new Mock<ISupplierRepository>();
        _mockMapper = new Mock<IMapper>();

        _purchaseOrderService = new PurchaseOrderService(
            _mockPurchaseOrderRepository.Object,
            _mockLineRepository.Object,
            _mockSupplierRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithExistingId_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new PurchaseOrder { Id = orderId, OrderNumber = "PO-001" };
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
            new PurchaseOrder { OrderNumber = "PO-001" },
            new PurchaseOrder { OrderNumber = "PO-002" }
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
            new PurchaseOrder { SupplierId = supplierId }
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
            new PurchaseOrder { Status = status }
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
        var supplier = new Supplier { Id = supplierId };
        var dto = new CreateUpdatePurchaseOrderDto
        {
            SupplierId = supplierId,
            Status = 0
        };
        var order = new PurchaseOrder
        {
            SupplierId = supplierId,
            Lines = new List<PurchaseOrderLine>
            {
                new PurchaseOrderLine { Quantity = 10, UnitPrice = 5.00m }
            }
        };
        var createdOrder = new PurchaseOrder { Id = Guid.NewGuid() };
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
        var order = new PurchaseOrder
        {
            Id = orderId,
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
        var updatedOrder = new PurchaseOrder();
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
        var newSupplier = new Supplier { Id = newSupplierId };
        var order = new PurchaseOrder
        {
            Id = orderId,
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
        var order = new PurchaseOrder { Id = orderId, Status = PurchaseOrderStatus.Draft };
        var newStatus = PurchaseOrderStatus.Approved;
        var updatedOrder = new PurchaseOrder { Status = newStatus };
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
        var order = new PurchaseOrder { Id = orderId };

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
}
