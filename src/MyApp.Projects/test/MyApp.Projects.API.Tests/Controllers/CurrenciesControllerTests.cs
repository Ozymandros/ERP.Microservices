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

public class CurrenciesControllerTests
{
    private readonly Mock<ICurrencyService> _service = new();
    private readonly CurrenciesController _sut;

    public CurrenciesControllerTests()
    {
        _sut = new CurrenciesController(_service.Object, new Mock<ICacheService>().Object, new Mock<ILogger<CurrenciesController>>().Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task ExportToPdf_ReturnsFile()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.ExportToPdf();
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        _service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var result = await _sut.GetAll();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCode_NotFound()
    {
        _service.Setup(s => s.GetByCodeAsync("XX", It.IsAny<CancellationToken>())).ReturnsAsync((CurrencyDto?)null);
        var result = await _sut.GetByCode("XX");
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}