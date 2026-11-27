using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Inventory.API.Controllers;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Inventory.API.Tests.Controllers;

public class WarehousesControllerTests
{
    private readonly Mock<IWarehouseService> _warehouseService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<WarehousesController>> _logger = new();
    private readonly WarehousesController _sut;

    public WarehousesControllerTests()
    {
        _sut = new WarehousesController(_warehouseService.Object, _logger.Object, _cacheService.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetAllWarehouses_NoQuery_Returns200()
    {
        var list = new List<WarehouseDto> { new(Guid.NewGuid()) { Name = "Main" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<WarehouseDto>>("all_warehouses"))
            .ReturnsAsync((IEnumerable<WarehouseDto>?)null);
        _warehouseService.Setup(s => s.GetAllWarehousesAsync()).ReturnsAsync(list);

        var result = await _sut.GetAllWarehouses(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWarehouseById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _warehouseService.Setup(s => s.GetWarehouseByIdAsync(id)).ReturnsAsync((WarehouseDto?)null);

        var result = await _sut.GetWarehouseById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWarehouseById_Found_ReturnsWarehouse()
    {
        var id = Guid.NewGuid();
        var dto = new WarehouseDto(id) { Name = "WH-1" };
        _warehouseService.Setup(s => s.GetWarehouseByIdAsync(id)).ReturnsAsync(dto);

        var result = await _sut.GetWarehouseById(id);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task ExportToXlsx_UsesServiceData_ReturnsFile()
    {
        var list = new List<WarehouseDto> { new(Guid.NewGuid()) { Name = "W" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<WarehouseDto>>("all_warehouses")).ReturnsAsync(list);

        var result = await _sut.ExportToXlsx();

        result.Should().BeOfType<FileContentResult>();
    }
}