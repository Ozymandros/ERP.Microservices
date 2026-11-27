using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Purchasing.API.Controllers;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Purchasing.API.Tests.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _service = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ILogger<SuppliersController>> _logger = new();
    private readonly SuppliersController _sut;

    public SuppliersControllerTests()
    {
        _sut = new SuppliersController(_service.Object, _cache.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static SupplierDto Sample() => new(Guid.NewGuid()) { Name = "Supp", Email = "s@v.com" };

    [Fact]
    public async Task ExportToXlsx_CacheHit_ReturnsFile()
    {
        _cache.Setup(c => c.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers")).ReturnsAsync([Sample()]);
        var result = await _sut.ExportToXlsx();
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task ExportToPdf_CacheMiss_CallsService()
    {
        _cache.Setup(c => c.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers")).ReturnsAsync((IEnumerable<SupplierDto>?)null);
        _service.Setup(s => s.GetAllSuppliersAsync()).ReturnsAsync([Sample()]);
        var result = await _sut.ExportToPdf();
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAllSuppliers_CacheHit()
    {
        var list = new List<SupplierDto> { Sample() };
        _cache.Setup(c => c.GetStateAsync<IEnumerable<SupplierDto>>("all_suppliers")).ReturnsAsync(list);
        var result = await _sut.GetAllSuppliers(new QuerySpec());
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllSuppliers_WithQuery_Queries()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "SearchTerm", "a" } });
        _service.Setup(s => s.QuerySuppliersAsync(It.IsAny<SupplierQuerySpec>()))
            .ReturnsAsync(new PaginatedResult<SupplierDto>([], 1, 10, 0));
        await _sut.GetAllSuppliers(new QuerySpec());
        _service.Verify(s => s.QuerySuppliersAsync(It.IsAny<SupplierQuerySpec>()), Times.Once);
    }

    [Fact]
    public async Task GetSupplierById_NotFound()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetStateAsync<SupplierDto>(It.IsAny<string>())).ReturnsAsync((SupplierDto?)null);
        _service.Setup(s => s.GetSupplierByIdAsync(id)).ReturnsAsync((SupplierDto?)null);
        var result = await _sut.GetSupplierById(id);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateSupplier_Returns201()
    {
        var dto = new CreateUpdateSupplierDto("Supp", "Contact", "s@v.com");
        var created = Sample();
        _service.Setup(s => s.CreateSupplierAsync(dto)).ReturnsAsync(created);
        _cache.Setup(c => c.RemoveStateAsync("all_suppliers")).Returns(Task.CompletedTask);
        var result = await _sut.CreateSupplier(dto);
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }
}