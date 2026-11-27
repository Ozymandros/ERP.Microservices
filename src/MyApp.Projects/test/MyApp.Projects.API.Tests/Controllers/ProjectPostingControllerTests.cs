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

public class ProjectPostingControllerTests
{
    private readonly Mock<IProjectPostingService> _service = new();
    private readonly ProjectPostingController _sut;

    public ProjectPostingControllerTests()
    {
        _sut = new ProjectPostingController(_service.Object, new Mock<ICacheService>().Object, new Mock<ILogger<ProjectPostingController>>().Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task ExportLedgerToXlsx_ReturnsFile()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.GetLedgerEntriesAsync(projectId, null, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ExportLedgerToXlsx(projectId);
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetLedgerEntries_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        _service.Setup(s => s.GetLedgerEntriesAsync(projectId, null, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetLedgerEntries(projectId, null);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PostUsage_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var dto = new PostUsageDto { ProjectTaskId = Guid.NewGuid(), ResourceId = Guid.NewGuid(), Quantity = 1, UnitCostLcy = 10m };
        _service.Setup(s => s.PostUsageAsync(projectId, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ProjectLedgerEntryDto(Guid.NewGuid()));
        var result = await _sut.PostUsage(projectId, dto);
        result.Should().BeOfType<OkObjectResult>();
    }
}