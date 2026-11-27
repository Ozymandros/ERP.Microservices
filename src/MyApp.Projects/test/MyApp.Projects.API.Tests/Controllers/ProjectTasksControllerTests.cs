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

public class ProjectTasksControllerTests
{
    private readonly Mock<IProjectTaskService> _service = new();
    private readonly ProjectTasksController _sut;

    public ProjectTasksControllerTests()
    {
        _sut = new ProjectTasksController(_service.Object, new Mock<ICacheService>().Object, new Mock<ILogger<ProjectTasksController>>().Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task ExportToXlsx_ReturnsFile()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.ListAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ExportToXlsx(projectId);
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.ListAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetAll(projectId);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(projectId, taskId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTaskDto?)null);
        var result = await _sut.GetById(projectId, taskId);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}