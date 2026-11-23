using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Permissions;
using Xunit;

namespace MyApp.Shared.Tests.Domain.Permissions;

public class PermissionCheckerTests
{
    private readonly Mock<IServiceInvoker> _invoker = new();
    private readonly Mock<IHttpContextAccessor> _httpContext = new();

    private PermissionChecker CreateSut() => new(_invoker.Object, _httpContext.Object);

    [Fact]
    public void Constructor_NullInvoker_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new PermissionChecker(null!, _httpContext.Object));

    [Fact]
    public void Constructor_NullHttpContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new PermissionChecker(_invoker.Object, null!));

    [Theory]
    [InlineData(null, "read")]
    [InlineData("", "read")]
    [InlineData("billing", null)]
    [InlineData("billing", "")]
    public async Task HasPermissionAsync_WithoutUserId_InvalidArgs_Throw(string? module, string? action)
    {
        var sut = CreateSut();
        _httpContext.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await sut.HasPermissionAsync(module!, action!));
    }

    [Fact]
    public async Task HasPermissionAsync_WithUserId_InvokesAuthService()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/internal/permissions/check");
        _invoker.Setup(i => i.CreateRequest(
                ServiceNames.Auth,
                "api/internal/permissions/check",
                HttpMethod.Get,
                null,
                It.IsAny<Dictionary<string, string?>>()))
            .Returns(request);
        _invoker.Setup(i => i.InvokeAsync<bool>(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().HasPermissionAsync(userId, "billing", "read");

        result.Should().BeTrue();
        _invoker.Verify(i => i.InvokeAsync<bool>(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenInvokerThrows_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/check");
        _invoker.Setup(i => i.CreateRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string?>>()))
            .Returns(request);
        _invoker.Setup(i => i.InvokeAsync<bool>(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("downstream"));

        var result = await CreateSut().HasPermissionAsync(userId, "sales", "write");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_FromHttpContext_WithAuthenticatedUser_UsesNameIdentifier()
    {
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ],
            authenticationType: "Test"))
        };
        _httpContext.Setup(h => h.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/check");
        _invoker.Setup(i => i.CreateRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string?>>()))
            .Returns(request);
        _invoker.Setup(i => i.InvokeAsync<bool>(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().HasPermissionAsync("inventory", "read");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_AttachesBearerTokenFromHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer my.jwt.token";
        _httpContext.Setup(h => h.HttpContext).Returns(httpContext);

        HttpRequestMessage? captured = null;
        _invoker.Setup(i => i.CreateRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string?>>()))
            .Returns(() =>
            {
                captured = new HttpRequestMessage(HttpMethod.Get, "http://localhost/check");
                return captured;
            });
        _invoker.Setup(i => i.InvokeAsync<bool>(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await CreateSut().HasPermissionAsync("crm", "write");

        captured.Should().NotBeNull();
        captured!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "my.jwt.token"));
    }
}