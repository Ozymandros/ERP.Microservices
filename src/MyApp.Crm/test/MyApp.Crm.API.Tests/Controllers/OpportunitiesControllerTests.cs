using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Crm.API.Controllers;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Crm.API.Tests.Controllers;

public class OpportunitiesControllerTests
{
    private readonly Mock<IOpportunityService> _service = new();
    private readonly Mock<ILogger<OpportunitiesController>> _logger = new();
    private readonly OpportunitiesController _sut;

    public OpportunitiesControllerTests()
    {
        _sut = new OpportunitiesController(_service.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static OpportunityDto Sample() =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, "Deal", "Prospect", 0.5m, 1000m, null, null, null, "owner", DateTime.UtcNow, null);

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
        _service.Setup(s => s.QueryAsync(It.IsAny<OpportunityQuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<OpportunityDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        _service.Verify(s => s.QueryAsync(It.IsAny<OpportunityQuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((OpportunityDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var dto = new CreateOpportunityDto(Guid.NewGuid(), "Deal", "owner", null);
        var created = Sample();
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task MarkWon_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var request = new MarkOpportunityWonRequest(null, false, null);
        _service.Setup(s => s.MarkWonAsync(id, request, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.MarkWon(id, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}