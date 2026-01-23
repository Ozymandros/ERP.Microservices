using Azure.Core;
using Microsoft.Extensions.Configuration;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Services;
using System.Net.Http.Json;

namespace MyApp.Tests.Integration;

public class AuthIntegrationTests
{
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var loginDto = new LoginDto
        (
            Email: "test@example.com",
            Password: "Password123!"
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var randomId = Guid.NewGuid();
        var email = $"test{randomId}@example.com";
        var password = "Password123!";

        var registerDto = new RegisterDto
        (
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            PasswordConfirm: password,
            Username: $"testuser{randomId}"
        );
        await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        var loginDto = new LoginDto
        (
            Email: email,
            Password: password
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized_SecondCase()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var loginDto = new LoginDto
        (
            Email: "invalid@example.com",
            Password: "wrongpassword"
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidModel_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var loginDto = new LoginDto
        (
            Email: "",
            Password: ""
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var registerDto = new RegisterDto
        (
            Email: $"test{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            FirstName: "John",
            LastName: "Doe",
            PasswordConfirm: "Password123!",
            Username: "johndoe"
            );

        var response = await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var randomId = Guid.NewGuid();
        var email = $"existing{randomId}@example.com";
        var password = "Password123!";
        var username = $"existinguser{randomId}";

        var registerDto = new RegisterDto
        (
            Email: email,
            Password: password,
            FirstName: "Jane",
            LastName: "Doe",
            PasswordConfirm: password,
            Username: username
        );

        // Initial registration
        await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        // Try to register again
        var response = await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidModel_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var registerDto = new RegisterDto
        (
            Email: "invalid-email",
            Password: "123",
            FirstName: "John",
            LastName: "Doe",
            Username: "us",
            PasswordConfirm: "456"
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsOk()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        // Register and Login to get tokens
        var randomId = Guid.NewGuid();
        var email = $"refresh{randomId}@example.com";
        var password = "Password123!";

        var registerDto = new RegisterDto(
             Email: email,
             Password: password,
             FirstName: "Test",
             LastName: "User",
             PasswordConfirm: password,
             Username: $"refreshuser{randomId}"
        );
        await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        var loginDto = new LoginDto(email, password);
        var loginResponse = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        var refreshTokenDto = new RefreshTokenDto
        (
            RefreshToken: tokens.RefreshToken,
            AccessToken: tokens.AccessToken
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/refresh", refreshTokenDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var refreshTokenDto = new RefreshTokenDto("invalid-access-token", "invalid-refresh-token");

        var response = await client.PostAsJsonAsync("/auth/api/auth/refresh", refreshTokenDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidModel_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var refreshTokenDto = new RefreshTokenDto
        (
            RefreshToken: "",
            AccessToken: ""
        );

        var response = await client.PostAsJsonAsync("/auth/api/auth/refresh", refreshTokenDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExternalLogin_WithValidProvider_ReturnsRedirect()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var client = app.CreateHttpClient("gateway");

        var provider = "Google";

        var response = await client.GetAsync($"/auth/api/auth/external-login/{provider}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExternalLogin_WithInvalidProvider_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var provider = "invalid-provider";

        var response = await client.GetAsync($"/auth/api/auth/external-login/{provider}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExternalLoginCallback_WithoutAuthentication_ReturnsBadRequest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        var response = await client.GetAsync("/auth/api/auth/external-callback");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    //TODO: Need a valid JWT Token
    //[Fact]
    //public async Task Logout_WithoutAuthorization_ReturnsUnauthorized()
    //{
    //    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
    //    appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    //    {
    //        clientBuilder.AddStandardResilienceHandler();
    //    });
    //    await using var app = await appHost.BuildAsync();
    //    await app.StartAsync();
    //    var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
    //    await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
    //        .WaitAsync(TimeSpan.FromSeconds(30));
    //    var client = app.CreateHttpClient("gateway");

    //    var response = await client.PostAsync("/auth/api/auth/logout", null);

    //    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    //}

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("auth-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        var client = app.CreateHttpClient("gateway");

        // Arrange
        // Register and Login
        var randomId = Guid.NewGuid();
        var email = $"logout{randomId}@example.com";
        var password = "Password123!";

        var registerDto = new RegisterDto(
             Email: email,
             Password: password,
             FirstName: "Test",
             LastName: "User",
             PasswordConfirm: password,
             Username: $"logoutuser{randomId}"
        );
        await client.PostAsJsonAsync("/auth/api/auth/register", registerDto);

        var loginDto = new LoginDto(email, password);
        var loginResponse = await client.PostAsJsonAsync("/auth/api/auth/login", loginDto);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        //Act
        var response = await client.PostAsync("/auth/api/auth/logout", null);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
