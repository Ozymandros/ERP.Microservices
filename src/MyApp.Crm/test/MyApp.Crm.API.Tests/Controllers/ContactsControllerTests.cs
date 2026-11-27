using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Crm.API.Controllers;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using Xunit;

namespace MyApp.Crm.API.Tests.Controllers;

public class ContactsControllerTests
{
    private readonly Mock<IContactService> _service = new();
    private readonly Mock<ILogger<ContactsController>> _logger = new();
    private readonly ContactsController _sut;

    public ContactsControllerTests()
    {
        _sut = new ContactsController(_service.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static ContactDto Sample() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Jane Doe", "j@test.com", null, null, true, true, DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_NoQuery_ReturnsEmptyArray()
    {
        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ContactDto>>();
    }

    [Fact]
    public async Task GetAll_WithQuery_Queries()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "page", "1" } });
        _service.Setup(s => s.QueryAsync(It.IsAny<QuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<ContactDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        _service.Verify(s => s.QueryAsync(It.IsAny<QuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ContactDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201()
    {
        var dto = new CreateContactDto(Guid.NewGuid(), "Jane", null, null, null, false);
        var created = Sample();
        _service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(dto, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }
}