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

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _productService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<ProductsController>> _logger = new();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_productService.Object, _logger.Object, _cacheService.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetAllProducts_NoQuery_ReturnsList()
    {
        var products = new List<ProductDto> { new(Guid.NewGuid()) { SKU = "SKU1", Name = "Item" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<ProductDto>>("all_products"))
            .ReturnsAsync((IEnumerable<ProductDto>?)null);
        _productService.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);

        var result = await _sut.GetAllProducts(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(products);
    }

    [Fact]
    public async Task GetProductById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<ProductDto>($"Product-{id}")).ReturnsAsync((ProductDto?)null);
        _productService.Setup(s => s.GetProductByIdAsync(id)).ReturnsAsync((ProductDto?)null);

        var result = await _sut.GetProductById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetProductById_CacheHit_ReturnsProduct()
    {
        var id = Guid.NewGuid();
        var product = new ProductDto(id) { Name = "Cached" };
        _cacheService.Setup(c => c.GetStateAsync<ProductDto>($"Product-{id}")).ReturnsAsync(product);

        var result = await _sut.GetProductById(id);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(product);
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        var products = new List<ProductDto> { new(Guid.NewGuid()) { SKU = "A", Name = "A" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<ProductDto>>("all_products")).ReturnsAsync(products);

        var result = await _sut.ExportToXlsx();

        result.Should().BeOfType<FileContentResult>();
    }
}