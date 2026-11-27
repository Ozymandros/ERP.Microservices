using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.API.Controllers;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using Xunit;

namespace MyApp.Inventory.API.Tests.Controllers;

public class WarehouseStocksControllerTests
{
    private readonly Mock<IWarehouseStockService> _stockService = new();
    private readonly Mock<ILogger<WarehouseStocksController>> _logger = new();
    private readonly WarehouseStocksController _sut;

    public WarehouseStocksControllerTests()
    {
        _sut = new WarehouseStocksController(_stockService.Object, _logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsStocks()
    {
        var stocks = new List<WarehouseStockDto> { new(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 5 } };
        _stockService.Setup(s => s.GetAllWarehouseStocksAsync()).ReturnsAsync(stocks);

        var result = await _sut.GetAll();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(stocks);
    }

    [Fact]
    public async Task GetByProductAndWarehouse_NotFound_Returns404()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        _stockService.Setup(s => s.GetByProductAndWarehouseAsync(productId, warehouseId)).ReturnsAsync((WarehouseStockDto?)null);

        var result = await _sut.GetByProductAndWarehouse(productId, warehouseId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        _stockService.Setup(s => s.GetAllWarehouseStocksAsync())
            .ReturnsAsync(new List<WarehouseStockDto> { new(Guid.NewGuid()) { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), AvailableQuantity = 1 } });

        var result = await _sut.ExportToXlsx();

        result.Should().BeOfType<FileContentResult>();
    }
}