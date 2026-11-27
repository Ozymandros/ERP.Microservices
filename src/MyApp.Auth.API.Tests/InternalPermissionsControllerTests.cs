using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Security;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class InternalPermissionsControllerTests
{
    private readonly Mock<IPermissionService> _permissionService = new();
    private readonly Mock<ILogSanitizer> _logSanitizer = new();
    private readonly Mock<ILogger<InternalPermissionsController>> _logger = new();
    private readonly InternalPermissionsController _sut;

    public InternalPermissionsControllerTests()
    {
        _logSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns("x");
        _sut = new InternalPermissionsController(_permissionService.Object, _logSanitizer.Object, _logger.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task Check_EmptyUserId_ReturnsBadRequest()
    {
        var result = await _sut.Check(Guid.Empty, "Users", "Read");

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Check_UntrustedCaller_ReturnsUnauthorized()
    {
        var result = await _sut.Check(Guid.NewGuid(), "Users", "Read");

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Check_TrustedCaller_ReturnsServiceResult()
    {
        var userId = Guid.NewGuid();
        _sut.Request.Headers["dapr-caller-app-id"] = new StringValues(ServiceNames.Inventory);
        _permissionService.Setup(s => s.HasPermissionAsync(userId, "Inventory", "Read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.Check(userId, "Inventory", "Read");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(true);
    }

    [Fact]
    public async Task Check_ServiceThrows_Returns500()
    {
        var userId = Guid.NewGuid();
        _sut.Request.Headers["dapr-caller-app-id"] = new StringValues(ServiceNames.Billing);
        _permissionService.Setup(s => s.HasPermissionAsync(userId, "Billing", "Read", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db down"));

        var result = await _sut.Check(userId, "Billing", "Read");

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}