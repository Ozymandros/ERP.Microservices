using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.API.Controllers;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Projects.API.Tests.Controllers;

public class ResourcesControllerTests
{
    private readonly Mock<IResourceService> _service = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly ResourcesController _sut;

    public ResourcesControllerTests()
    {
        _sut = new ResourcesController(_service.Object, _cache.Object, new Mock<ILogger<ResourcesController>>().Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ExportToXlsx();
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAll_CacheMiss_CallsService()
    {
        _cache.Setup(c => c.GetStateAsync<List<ResourceDto>>("all_project_resources")).ReturnsAsync((List<ResourceDto>?)null);
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetAll(new QuerySpec());
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ResourceDto?)null);
        var result = await _sut.GetById(id);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}