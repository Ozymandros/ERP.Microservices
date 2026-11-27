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

public class AccountsControllerTests
{
    private readonly Mock<IAccountService> _service = new();
    private readonly Mock<ILogger<AccountsController>> _logger = new();
    private readonly AccountsController _sut;

    public AccountsControllerTests()
    {
        _sut = new AccountsController(_service.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static AccountDto Sample() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Acme", "TAX", null, null, true, "owner", DateTimeOffset.UtcNow, DateTime.UtcNow, null);

    [Fact]
    public async Task GetAll_NoQuery_ReturnsList()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Sample()]);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_WithQuery_Queries()
    {
        _sut.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { "page", "1" } });
        _service.Setup(s => s.QueryAsync(It.IsAny<QuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<AccountDto>([], 1, 10, 0));

        await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        _service.Verify(s => s.QueryAsync(It.IsAny<QuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByTaxId_NotFound()
    {
        _service.Setup(s => s.GetByTaxIdAsync("X", It.IsAny<CancellationToken>())).ReturnsAsync((AccountDto?)null);

        var result = await _sut.GetByTaxId("X", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Found()
    {
        var customerId = Guid.NewGuid();
        _service.Setup(s => s.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.GetByCustomerId(customerId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateOwner_NotFound()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAccountOwnerDto("newowner");
        _service.Setup(s => s.UpdateOwnerAsync(id, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("missing"));

        var result = await _sut.UpdateOwner(id, dto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}