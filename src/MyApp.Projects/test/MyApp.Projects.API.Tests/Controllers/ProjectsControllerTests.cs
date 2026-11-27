using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Projects.API.Controllers;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Projects.API.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _service = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ILogger<ProjectsController>> _logger = new();
    private readonly ProjectsController _sut;

    public ProjectsControllerTests()
    {
        _sut = new ProjectsController(_service.Object, _cache.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static ProjectDto Sample(Guid? id = null) =>
        new(id ?? Guid.NewGuid()) { Description = "Proj", CustomerId = Guid.NewGuid(), ProjectNumber = "P-1", Status = "Planning", CurrencyCode = "USD" };

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Sample()]);

        var result = await _sut.ExportToXlsx();

        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task ExportToPdf_ReturnsFile()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Sample()]);

        var result = await _sut.ExportToPdf();

        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAll_NoQuery_CacheHit()
    {
        var list = new List<ProjectDto> { Sample() };
        _cache.Setup(c => c.GetStateAsync<List<ProjectDto>>("all_projects")).ReturnsAsync(list);

        var result = await _sut.GetAll(new QuerySpec());

        result.Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WithQuery_QueriesService()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "SearchTerm", "p" } });
        _service.Setup(s => s.QueryAsync(It.IsAny<ProjectQuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<ProjectDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec());

        _service.Verify(s => s.QueryAsync(It.IsAny<ProjectQuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CacheHit()
    {
        var id = Guid.NewGuid();
        var dto = Sample(id);
        _cache.Setup(c => c.GetStateAsync<ProjectDto>($"project_{id}")).ReturnsAsync(dto);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetStateAsync<ProjectDto>(It.IsAny<string>())).ReturnsAsync((ProjectDto?)null);
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectDto?)null);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_InvalidatesListCache()
    {
        var dto = new CreateProjectDto { Description = "New", CustomerId = Guid.NewGuid(), CurrencyCode = "USD" };
        var created = Sample();
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        _cache.Setup(c => c.RemoveStateAsync("all_projects")).Returns(Task.CompletedTask);

        var result = await _sut.Create(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }
}