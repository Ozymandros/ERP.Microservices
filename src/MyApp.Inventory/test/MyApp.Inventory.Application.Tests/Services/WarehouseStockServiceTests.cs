using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Application.Tests.Common;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Services;

public class WarehouseStockServiceTests : BaseServiceTest
{
    private readonly Mock<IWarehouseStockRepository> _mockWarehouseStockRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IInventoryTransactionRepository> _mockTransactionRepository;
    private readonly Mock<ILogger<WarehouseStockService>> _mockLogger;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly WarehouseStockService _service;

    public WarehouseStockServiceTests()
    {
        _mockWarehouseStockRepository = new Mock<IWarehouseStockRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockTransactionRepository = new Mock<IInventoryTransactionRepository>();
        _mockLogger = CreateMockLogger<WarehouseStockService>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _service = new WarehouseStockService(
            _mockWarehouseStockRepository.Object,
            _mockProductRepository.Object,
            _mockTransactionRepository.Object,
            Mapper,
            _mockLogger.Object,
            _mockEventPublisher.Object);
    }

    #region GetByProductAndWarehouseAsync Tests

    [Fact]
    public async Task GetByProductAndWarehouseAsync_WithExistingStock_ReturnsWarehouseStockDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 100,
            ReservedQuantity = 10,
            OnOrderQuantity = 5
        };
        var expectedDto = new WarehouseStockDto(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 100,
            ReservedQuantity = 10,
            OnOrderQuantity = 5
        };

        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(stock);
        MockMapper.Setup(m => m.Map<WarehouseStockDto>(stock)).Returns(expectedDto);

        // Act
        var result = await _service.GetByProductAndWarehouseAsync(productId, warehouseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _mockWarehouseStockRepository.Verify(r => r.GetByProductAndWarehouseAsync(productId, warehouseId), Times.Once);
    }

    [Fact]
    public async Task GetByProductAndWarehouseAsync_WithNonExistentStock_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync((WarehouseStock?)null);

        // Act
        var result = await _service.GetByProductAndWarehouseAsync(productId, warehouseId);

        // Assert
        result.Should().BeNull();
        _mockWarehouseStockRepository.Verify(r => r.GetByProductAndWarehouseAsync(productId, warehouseId), Times.Once);
    }

    #endregion

    #region GetByProductIdAsync Tests

    [Fact]
    public async Task GetByProductIdAsync_WithExistingStocks_ReturnsListOfWarehouseStockDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var stocks = new List<WarehouseStock>
        {
            new WarehouseStock(Guid.NewGuid()) { ProductId = productId, WarehouseId = Guid.NewGuid(), AvailableQuantity = 50 },
            new WarehouseStock(Guid.NewGuid()) { ProductId = productId, WarehouseId = Guid.NewGuid(), AvailableQuantity = 75 }
        };
        var expectedDtos = stocks.Select(s => new WarehouseStockDto(s.Id) { ProductId = s.ProductId, WarehouseId = s.WarehouseId, AvailableQuantity = s.AvailableQuantity }).ToList();

        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(stocks);
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(stocks)).Returns(expectedDtos);

        // Act
        var result = await _service.GetByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedDtos);
        _mockWarehouseStockRepository.Verify(r => r.GetByProductIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetByProductIdAsync_WithNoStocks_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<WarehouseStock>());
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(It.IsAny<List<WarehouseStock>>()))
            .Returns(new List<WarehouseStockDto>());

        // Act
        var result = await _service.GetByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockWarehouseStockRepository.Verify(r => r.GetByProductIdAsync(productId), Times.Once);
    }

    #endregion

    #region GetByWarehouseIdAsync Tests

    [Fact]
    public async Task GetByWarehouseIdAsync_WithExistingStocks_ReturnsListOfWarehouseStockDto()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var stocks = new List<WarehouseStock>
        {
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = warehouseId, AvailableQuantity = 30 },
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = warehouseId, AvailableQuantity = 40 }
        };
        var expectedDtos = stocks.Select(s => new WarehouseStockDto(s.Id) { ProductId = s.ProductId, WarehouseId = s.WarehouseId, AvailableQuantity = s.AvailableQuantity }).ToList();

        _mockWarehouseStockRepository
            .Setup(r => r.GetByWarehouseIdAsync(warehouseId))
            .ReturnsAsync(stocks);
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(stocks)).Returns(expectedDtos);

        // Act
        var result = await _service.GetByWarehouseIdAsync(warehouseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedDtos);
        _mockWarehouseStockRepository.Verify(r => r.GetByWarehouseIdAsync(warehouseId), Times.Once);
    }

    #endregion

    #region GetProductAvailabilityAsync Tests

    [Fact]
    public async Task GetProductAvailabilityAsync_WithExistingProduct_ReturnsStockAvailabilityDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var stocks = new List<WarehouseStock>
        {
            new WarehouseStock(Guid.NewGuid()) { ProductId = productId, AvailableQuantity = 50, ReservedQuantity = 10, OnOrderQuantity = 5 },
            new WarehouseStock(Guid.NewGuid()) { ProductId = productId, AvailableQuantity = 30, ReservedQuantity = 5, OnOrderQuantity = 2 }
        };
        var stockDtos = stocks.Select(s => new WarehouseStockDto(s.Id) { ProductId = s.ProductId, AvailableQuantity = s.AvailableQuantity, ReservedQuantity = s.ReservedQuantity, OnOrderQuantity = s.OnOrderQuantity }).ToList();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository.Setup(r => r.GetByProductIdAsync(productId)).ReturnsAsync(stocks);
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(stocks)).Returns(stockDtos);

        // Act
        var result = await _service.GetProductAvailabilityAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.SKU.Should().Be("PROD-001");
        result.ProductName.Should().Be("Test Product");
        result.TotalAvailable.Should().Be(80); // 50 + 30
        result.TotalReserved.Should().Be(15); // 10 + 5
        result.TotalOnOrder.Should().Be(7); // 5 + 2
        result.WarehouseStocks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProductAvailabilityAsync_WithNonExistentProduct_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductAvailabilityAsync(productId);

        // Assert
        result.Should().BeNull();
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _mockWarehouseStockRepository.Verify(r => r.GetByProductIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region ReserveStockAsync Tests

    [Fact]
    public async Task ReserveStockAsync_WithValidDto_ReservesStockAndReturnsReservationDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var warehouse = new Warehouse(warehouseId) { Name = "Main Warehouse" };
        var warehouseStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 100,
            ReservedQuantity = 0,
            Warehouse = warehouse
        };
        var dto = new ReserveStockDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 20,
            OrderId = orderId
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(warehouseStock);
        _mockWarehouseStockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReserveStockAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.WarehouseId.Should().Be(warehouseId);
        result.OrderId.Should().Be(orderId);
        result.Quantity.Should().Be(20);
        result.Status.Should().Be("Reserved");

        _mockWarehouseStockRepository.Verify(r => r.UpdateAsync(It.Is<WarehouseStock>(s =>
            s.AvailableQuantity == 80 && s.ReservedQuantity == 20)), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync("inventory.stock.reserved", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ReserveStockAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _service.ReserveStockAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReserveStockAsync_WithNonExistentStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var dto = new ReserveStockDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 20,
            OrderId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync((WarehouseStock?)null);

        // Act
        Func<Task> act = async () => await _service.ReserveStockAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*No stock record found*");
    }

    [Fact]
    public async Task ReserveStockAsync_WithInsufficientStock_ThrowsInsufficientStockException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var warehouseStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 10,
            ReservedQuantity = 0
        };
        var dto = new ReserveStockDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 20,
            OrderId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(warehouseStock);

        // Act
        Func<Task> act = async () => await _service.ReserveStockAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InsufficientStockException>()
            .Where(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.RequestedQuantity == 20 && e.AvailableQuantity == 10);
    }

    [Fact]
    public async Task ReserveStockAsync_WhenEventPublishingFails_StillReturnsReservation()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var warehouseStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 100,
            ReservedQuantity = 0
        };
        var dto = new ReserveStockDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 20,
            OrderId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(warehouseStock);
        _mockWarehouseStockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Event publishing failed"));

        // Act
        var result = await _service.ReserveStockAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(20);
        _mockWarehouseStockRepository.Verify(r => r.UpdateAsync(It.IsAny<WarehouseStock>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ReleaseReservationAsync Tests

    [Fact]
    public async Task ReleaseReservationAsync_WithValidReservationId_PublishesEvent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ReleaseReservationAsync(reservationId);

        // Assert
        _mockEventPublisher.Verify(e => e.PublishAsync("inventory.stock.released", It.IsAny<object>()), Times.Once);
        VerifyLoggerCalledAtLeast(_mockLogger, LogLevel.Information, 1);
    }

    [Fact]
    public async Task ReleaseReservationAsync_WhenEventPublishingFails_LogsError()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Event publishing failed"));

        // Act
        await _service.ReleaseReservationAsync(reservationId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region TransferStockAsync Tests

    [Fact]
    public async Task TransferStockAsync_WithValidDto_TransfersStockBetweenWarehouses()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fromWarehouseId = Guid.NewGuid();
        var toWarehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var sourceStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            AvailableQuantity = 100,
            ReservedQuantity = 0,
            Warehouse = new Warehouse(fromWarehouseId) { Name = "Source Warehouse" }
        };
        var dto = new StockTransferDto
        {
            ProductId = productId,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Quantity = 30,
            Reason = "Restocking"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromWarehouseId))
            .ReturnsAsync(sourceStock);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, toWarehouseId))
            .ReturnsAsync((WarehouseStock?)null);
        _mockWarehouseStockRepository
            .Setup(r => r.AddAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockWarehouseStockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockTransactionRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryTransaction>()))
            .ReturnsAsync((InventoryTransaction transaction) => transaction);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.TransferStockAsync(dto);

        // Assert
        _mockWarehouseStockRepository.Verify(r => r.UpdateAsync(It.Is<WarehouseStock>(s =>
            s.WarehouseId == fromWarehouseId && s.AvailableQuantity == 70)), Times.Once);
        _mockWarehouseStockRepository.Verify(r => r.AddAsync(It.Is<WarehouseStock>(s =>
            s.WarehouseId == toWarehouseId && s.AvailableQuantity == 30)), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddAsync(It.IsAny<InventoryTransaction>()), Times.Exactly(2));
        _mockEventPublisher.Verify(e => e.PublishAsync("inventory.stock.transferred", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task TransferStockAsync_WithExistingDestinationStock_UpdatesDestinationStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fromWarehouseId = Guid.NewGuid();
        var toWarehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var sourceStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            AvailableQuantity = 100
        };
        var destStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = toWarehouseId,
            AvailableQuantity = 50
        };
        var dto = new StockTransferDto
        {
            ProductId = productId,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Quantity = 30,
            Reason = "Restocking"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromWarehouseId))
            .ReturnsAsync(sourceStock);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, toWarehouseId))
            .ReturnsAsync(destStock);
        _mockWarehouseStockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockTransactionRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryTransaction>()))
            .ReturnsAsync((InventoryTransaction transaction) => transaction);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.TransferStockAsync(dto);

        // Assert
        _mockWarehouseStockRepository.Verify(r => r.AddAsync(It.IsAny<WarehouseStock>()), Times.Never);
        _mockWarehouseStockRepository.Verify(r => r.UpdateAsync(It.Is<WarehouseStock>(s =>
            s.WarehouseId == toWarehouseId && s.AvailableQuantity == 80)), Times.Once);
    }

    [Fact]
    public async Task TransferStockAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _service.TransferStockAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TransferStockAsync_WithInsufficientStock_ThrowsStockTransferException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fromWarehouseId = Guid.NewGuid();
        var toWarehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var sourceStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            AvailableQuantity = 10
        };
        var dto = new StockTransferDto
        {
            ProductId = productId,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Quantity = 30,
            Reason = "Restocking"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, fromWarehouseId))
            .ReturnsAsync(sourceStock);

        // Act
        Func<Task> act = async () => await _service.TransferStockAsync(dto);

        // Assert
        await act.Should().ThrowAsync<StockTransferException>()
            .Where(e => e.ProductId == productId && e.FromWarehouseId == fromWarehouseId && e.ToWarehouseId == toWarehouseId);
    }

    #endregion

    #region AdjustStockAsync Tests

    [Fact]
    public async Task AdjustStockAsync_WithValidDto_AdjustsStockAndCreatesTransaction()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var warehouseStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 100,
            Warehouse = new Warehouse(warehouseId) { Name = "Main Warehouse" }
        };
        var dto = new StockAdjustmentDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            QuantityChange = -10,
            Reason = "Damage",
            Reference = "ADJ-001"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(warehouseStock);
        _mockWarehouseStockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WarehouseStock>()))
            .ReturnsAsync((WarehouseStock stock) => stock);
        _mockTransactionRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryTransaction>()))
            .ReturnsAsync((InventoryTransaction transaction) => transaction);
        _mockEventPublisher
            .Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.AdjustStockAsync(dto);

        // Assert
        _mockWarehouseStockRepository.Verify(r => r.UpdateAsync(It.Is<WarehouseStock>(s =>
            s.AvailableQuantity == 90)), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddAsync(It.Is<InventoryTransaction>(t =>
            t.QuantityChange == -10 && t.TransactionType == TransactionType.Adjustment)), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishAsync("inventory.stock.adjusted", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task AdjustStockAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _service.AdjustStockAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AdjustStockAsync_WithNonExistentStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var dto = new StockAdjustmentDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            QuantityChange = -10,
            Reason = "Damage"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync((WarehouseStock?)null);

        // Act
        Func<Task> act = async () => await _service.AdjustStockAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*No stock record found*");
    }

    [Fact]
    public async Task AdjustStockAsync_WithAdjustmentResultingInNegativeStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PROD-001", Name = "Test Product" };
        var warehouseStock = new WarehouseStock(Guid.NewGuid())
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableQuantity = 5
        };
        var dto = new StockAdjustmentDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            QuantityChange = -10,
            Reason = "Damage"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockWarehouseStockRepository
            .Setup(r => r.GetByProductAndWarehouseAsync(productId, warehouseId))
            .ReturnsAsync(warehouseStock);

        // Act
        Func<Task> act = async () => await _service.AdjustStockAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*negative stock*");
    }

    #endregion

    #region GetLowStockAsync Tests

    [Fact]
    public async Task GetLowStockAsync_ReturnsListOfLowStockWarehouseStockDto()
    {
        // Arrange
        var lowStocks = new List<WarehouseStock>
        {
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 5 },
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 3 }
        };
        var expectedDtos = lowStocks.Select(s => new WarehouseStockDto(s.Id) { ProductId = s.ProductId, WarehouseId = s.WarehouseId, AvailableQuantity = s.AvailableQuantity }).ToList();

        _mockWarehouseStockRepository
            .Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(lowStocks);
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(lowStocks)).Returns(expectedDtos);

        // Act
        var result = await _service.GetLowStockAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedDtos);
        _mockWarehouseStockRepository.Verify(r => r.GetLowStockAsync(), Times.Once);
    }

    #endregion

    #region GetAllWarehouseStocksAsync Tests

    [Fact]
    public async Task GetAllWarehouseStocksAsync_ReturnsListOfAllWarehouseStockDto()
    {
        // Arrange
        var stocks = new List<WarehouseStock>
        {
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 100 },
            new WarehouseStock(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 200 }
        };
        var expectedDtos = stocks.Select(s => new WarehouseStockDto(s.Id) { ProductId = s.ProductId, WarehouseId = s.WarehouseId, AvailableQuantity = s.AvailableQuantity }).ToList();

        _mockWarehouseStockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(stocks);
        MockMapper.Setup(m => m.Map<List<WarehouseStockDto>>(stocks)).Returns(expectedDtos);

        // Act
        var result = await _service.GetAllWarehouseStocksAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedDtos);
        _mockWarehouseStockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion
}
