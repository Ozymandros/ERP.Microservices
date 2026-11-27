using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.API.Controllers;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Exceptions;
using Xunit;

namespace MyApp.Inventory.API.Tests.Controllers;

public class StockOperationsControllerTests
{
    private readonly Mock<IWarehouseStockService> _stockService = new();
    private readonly Mock<ILogger<StockOperationsController>> _logger = new();
    private readonly StockOperationsController _sut;

    public StockOperationsControllerTests()
    {
        _sut = new StockOperationsController(_stockService.Object, _logger.Object);
    }

    [Fact]
    public async Task ReserveStock_InsufficientStock_ReturnsBadRequest()
    {
        var dto = new ReserveStockDto { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), Quantity = 10, OrderId = Guid.NewGuid() };
        _stockService.Setup(s => s.ReserveStockAsync(dto))
            .ThrowsAsync(new InsufficientStockException(dto.ProductId, dto.WarehouseId, dto.Quantity, 2));

        var result = await _sut.ReserveStock(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReleaseReservation_NotFound_Returns404()
    {
        var reservationId = Guid.NewGuid();
        _stockService.Setup(s => s.ReleaseReservationAsync(reservationId))
            .ThrowsAsync(new KeyNotFoundException("missing"));

        var result = await _sut.ReleaseReservation(reservationId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AdjustStock_InvalidOperation_ReturnsBadRequest()
    {
        var dto = new StockAdjustmentDto { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), QuantityChange = -5 };
        _stockService.Setup(s => s.AdjustStockAsync(dto)).ThrowsAsync(new InvalidOperationException("bad"));

        var result = await _sut.AdjustStock(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AdjustStock_Success_ReturnsNoContent()
    {
        var dto = new StockAdjustmentDto { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), QuantityChange = 1 };
        _stockService.Setup(s => s.AdjustStockAsync(dto)).Returns(Task.CompletedTask);

        var result = await _sut.AdjustStock(dto);

        result.Should().BeOfType<NoContentResult>();
    }
}