using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Domain.Security;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<ILogSanitizer> _logSanitizer = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _logSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns("***");
        _sut = new AuthController(_authService.Object, _logSanitizer.Object, _logger.Object);
    }

    [Fact]
    public async Task Login_InvalidModel_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Required");

        var result = await _sut.Login(new LoginDto("user@test.com", "password1"));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _authService.Verify(s => s.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var dto = new LoginDto("user@test.com", "password1");
        _authService.Setup(s => s.LoginAsync(dto)).ReturnsAsync((TokenResponseDto?)null);

        var result = await _sut.Login(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_Success_ReturnsOkWithTokens()
    {
        var dto = new LoginDto("user@test.com", "password1");
        var tokens = new TokenResponseDto("access", "refresh", 3600);
        _authService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(tokens);

        var result = await _sut.Login(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(tokens);
    }

    [Fact]
    public async Task RefreshToken_MissingTokens_ReturnsBadRequest()
    {
        var result = await _sut.RefreshToken(new RefreshTokenDto("", ""));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _authService.Verify(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_InvalidRefresh_ReturnsUnauthorized()
    {
        var dto = new RefreshTokenDto("access", "refresh");
        _authService.Setup(s => s.RefreshTokenAsync(dto)).ReturnsAsync((TokenResponseDto?)null);

        var result = await _sut.RefreshToken(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}