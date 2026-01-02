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
    public async Task Login_WithVInvalidCredentials_ReturnsUnauthorized()
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

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

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

        var loginDto = new LoginDto
        (
            Email: "admin@myapp.local",
            Password: "Admin123!"
        );

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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
            Email: "invalid@example.com",
            Password: "wrongpassword"
        );

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

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

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

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

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);

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

        var registerDto = new RegisterDto
        (
            Email: "existing@example.com",
            Password: "Password123!",
            FirstName: "Jane",
            LastName: "Doe",
            PasswordConfirm: "Password123!",
            Username: "existinguser"
        );

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);

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

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);

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

        var refreshTokenDto = new RefreshTokenDto
        (
            RefreshToken: "valid-refresh-token",
            AccessToken: "valid-access-token"
        );

        var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshTokenDto);

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

        var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshTokenDto);

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

        var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshTokenDto);

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

        var response = await client.GetAsync($"/api/auth/external-login/{provider}");

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

        var response = await client.GetAsync($"/api/auth/external-login/{provider}");

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

        var response = await client.GetAsync("/api/auth/external-callback");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    //TODO: Fata un Token valid de JWT
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

    //    var response = await client.PostAsync("/api/auth/logout", null);

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
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User"
        };

        IConfiguration configuration = app.Services.GetRequiredService<IConfiguration>();
        var token = await new JwtTokenProvider(configuration).GenerateAccessTokenAsync(user);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-jwt-token");

        //Act
        var response = await client.PostAsync("/api/auth/logout", null);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
