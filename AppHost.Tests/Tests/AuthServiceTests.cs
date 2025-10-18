using AppHost.Tests.Base;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace AppHost.Tests.Tests;

public class AuthServiceTests : BaseIntegrationTest
{
    public AuthServiceTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("auth-service");
        var user = new
        {
            Email = $"test{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // Act
        var response = await GatewayClient.PostAsJsonAsync("/auth/api/auth/register", user);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        await WaitForServiceAsync("auth-service");
        var credentials = new
        {
            Email = "admin@test.com",
            Password = "Admin123!"
        };

        // Act
        var response = await GatewayClient.PostAsJsonAsync("/auth/api/auth/login", credentials);
        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content?.AccessToken);
        Assert.NotNull(content?.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        await WaitForServiceAsync("auth-service");
        var client = await CreateAuthorizedClientAsync();

        // Act
        var response = await client.PostAsync("/auth/api/auth/refresh", null);
        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content?.AccessToken);
        Assert.NotNull(content?.RefreshToken);
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}