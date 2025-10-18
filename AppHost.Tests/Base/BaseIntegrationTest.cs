using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace AppHost.Tests.Base;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly ITestOutputHelper _output;
    protected DistributedApplication App { get; private set; }
    protected HttpClient GatewayClient { get; private set; }
    protected ResourceNotificationService ResourceNotification { get; private set; }
    protected ILogger Logger { get; private set; }

    protected BaseIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    protected virtual async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        
        // Configure resilience and logging
        appHost.Services.ConfigureHttpClientDefaults(builder =>
        {
            builder.AddStandardResilienceHandler();
        });

        appHost.Services.AddLogging(builder =>
        {
            builder.ClearProviders(); // Remove default providers
            builder.AddProvider(new XunitLoggerProvider(_output)); // Your custom provider
        });

        // Add xUnit logging
        appHost.Services.AddLogging(builder =>
        {
            builder.AddXunit(_output);
        });

        App = await appHost.BuildAsync();
        ResourceNotification = App.Services.GetRequiredService<ResourceNotificationService>();
        Logger = App.Services.GetRequiredService<ILogger<BaseIntegrationTest>>();
        
        await App.StartAsync();
        
        // Initialize gateway client
        GatewayClient = App.CreateHttpClient("gateway");
        await ResourceNotification.WaitForResourceAsync("gateway", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
    }

    public async Task DisposeAsync()
    {
        if (App != null)
        {
            await App.DisposeAsync();
        }
    }

    protected async Task<string> GetAuthTokenAsync()
    {
        var credentials = new
        {
            Email = "admin@test.com",
            Password = "Admin123!"
        };

        var response = await GatewayClient.PostAsJsonAsync("/auth/api/auth/login", credentials);
        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
        
        return content?.AccessToken;
    }

    protected async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = App.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task WaitForServiceAsync(string serviceName, int timeoutSeconds = 30)
    {
        await ResourceNotification.WaitForResourceAsync(serviceName, KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));
    }

    Task IAsyncLifetime.InitializeAsync() => InitializeAsync();

    private class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}