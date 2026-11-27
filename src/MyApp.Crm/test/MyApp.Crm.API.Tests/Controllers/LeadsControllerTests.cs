using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Crm.API.Controllers;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Crm.API.Tests.Controllers;

public class LeadsControllerTests
{
    private readonly Mock<ILeadService> _service = new();
    private readonly Mock<ILogger<LeadsController>> _logger = new();
    private readonly LeadsController _sut;

    public LeadsControllerTests()
    {
        _sut = new LeadsController(_service.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static LeadDto Sample(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Lead", null, null, null, null, null, "New", "owner", DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_NoQuery_ReturnsList()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Sample()]);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithQuery_QueriesService()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "SearchTerm", "x" } });
        _service.Setup(s => s.QueryAsync(It.IsAny<LeadQuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<LeadDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        _service.Verify(s => s.QueryAsync(It.IsAny<LeadQuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((LeadDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var dto = new CreateLeadDto("Title", "owner", null, null, null, null);
        var created = Sample();
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_NotFound()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateLeadDto("Title", null, null, null, null);
        _service.Setup(s => s.UpdateAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("missing"));

        var result = await _sut.Update(id, dto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}