using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Projects.API.Controllers;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using Xunit;

namespace MyApp.Projects.API.Tests.Controllers;

public class ProjectPlanningLinesControllerTests
{
    private readonly Mock<IPlanningLineService> _service = new();
    private readonly ProjectPlanningLinesController _sut;

    public ProjectPlanningLinesControllerTests()
    {
        _sut = new ProjectPlanningLinesController(_service.Object, new Mock<ICacheService>().Object, new Mock<ILogger<ProjectPlanningLinesController>>().Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.ListAsync(projectId, null, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ExportToXlsx(projectId);
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.ListAsync(projectId, null, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetAll(projectId, null);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var projectId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(projectId, lineId, It.IsAny<CancellationToken>())).ReturnsAsync((PlanningLineDto?)null);
        var result = await _sut.GetById(projectId, lineId);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}