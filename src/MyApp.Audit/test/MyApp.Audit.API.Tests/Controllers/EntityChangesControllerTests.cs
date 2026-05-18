using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Audit.API.Controllers;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Application.Contracts.Services;
using MyApp.Audit.Domain;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Audit.API.Tests.Controllers;

public class EntityChangesControllerTests
{
    private readonly Mock<IEntityChangeService> _service = new();
    private readonly Mock<ILogger<EntityChangesController>> _logger = new();
    private readonly EntityChangesController _sut;

    public EntityChangesControllerTests()
    {
        _sut = new EntityChangesController(_service.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EntityChangeDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByEntity_ReturnsOk()
    {
        var entityId = Guid.NewGuid();
        var list = new List<EntityChangeDto> { SampleDto(Guid.NewGuid()) };
        _service.Setup(s => s.GetByEntityAsync("Product", entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _sut.GetByEntity("Product", entityId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(list);
    }

    [Fact]
    public async Task Record_ValidDto_ReturnsCreated()
    {
        var dto = new CreateEntityChangeDto
        {
            EntityName = "Product",
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeTypeEnum.Created
        };
        var created = SampleDto(Guid.NewGuid());
        _service.Setup(s => s.RecordAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Record(dto, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeSameAs(created);
        createdResult.ActionName.Should().Be(nameof(EntityChangesController.GetById));
    }

    [Fact]
    public async Task Query_WithQueryString_ReturnsPaginatedOk()
    {
        _sut.HttpContext.Request.QueryString = new QueryString("?page=1&pageSize=10&EntityName=Product");

        var page = new PaginatedResult<EntityChangeDto>(
            [SampleDto(Guid.NewGuid())], 1, 10, 1);

        _service.Setup(s => s.QueryAsync(It.IsAny<MyApp.Shared.Domain.Specifications.ISpecification<MyApp.Audit.Domain.EntityChange>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _sut.Query(new QuerySpec(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(page);
    }

    private static EntityChangeDto SampleDto(Guid id) =>
        new(id, "Product", Guid.NewGuid(), "Updated", null, null,
            DateTime.UtcNow, "user", null, null, []);
}
