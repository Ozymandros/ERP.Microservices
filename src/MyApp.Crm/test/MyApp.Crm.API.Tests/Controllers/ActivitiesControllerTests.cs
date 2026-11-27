using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Crm.API.Controllers;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Crm.API.Tests.Controllers;

public class ActivitiesControllerTests
{
    private readonly Mock<IActivityService> _service = new();
    private readonly Mock<ILogger<ActivitiesController>> _logger = new();
    private readonly ActivitiesController _sut;

    public ActivitiesControllerTests()
    {
        _sut = new ActivitiesController(_service.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static ActivityDto Sample() =>
        new(Guid.NewGuid(), "Call", "Phone", "Open", DateTimeOffset.UtcNow, null, "owner", null, null, null, DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_ReturnsList()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Sample()]);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithQuery_Queries()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "page", "1" } });
        _service.Setup(s => s.QueryAsync(It.IsAny<ActivityQuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<ActivityDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        _service.Verify(s => s.QueryAsync(It.IsAny<ActivityQuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ActivityDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var dto = new CreateActivityDto("Call", "Phone", DateTimeOffset.UtcNow, "owner", null, null, null);
        var created = Sample();
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Complete_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new CompleteActivityDto("done");
        _service.Setup(s => s.CompleteAsync(id, dto, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.Complete(id, dto, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}