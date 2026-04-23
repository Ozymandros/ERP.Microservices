using FluentAssertions;
using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Messaging;
using Xunit;

// Minimal no-op stub so plugin classes can be instantiated without Dapr in tests.
file sealed class NullServiceInvoker : IServiceInvoker
{
    public Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string serviceName, string methodPath, HttpMethod httpMethod, TRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(default(TResponse)!);

    public Task<TResponse> InvokeAsync<TResponse>(
        string serviceName, string methodPath, HttpMethod httpMethod,
        CancellationToken cancellationToken = default)
        => Task.FromResult(default(TResponse)!);

    public Task InvokeAsync(
        string serviceName, string methodPath, HttpMethod httpMethod,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<TResponse> InvokeAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(default(TResponse)!);

    public HttpRequestMessage CreateRequest(
        string serviceName, string methodPath, HttpMethod httpMethod,
        object? requestBody = null, Dictionary<string, string?>? queryParams = null)
        => new(httpMethod, $"http://localhost/{methodPath}");
}

public class KernelIntegrationTests : IAsyncLifetime
{
    private Kernel _kernel = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var deepseekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrWhiteSpace(deepseekKey))
        {
            kernelBuilder.AddOpenAIChatCompletion("gpt-4o-mini", deepseekKey);
        }

        _kernel = kernelBuilder.Build();

        // Import the Orders plugin using the SK 1.x API
        var ordersPlugin = new OrdersPlugin(new NullServiceInvoker());
        _kernel.ImportPluginFromObject(ordersPlugin, "Orders");

        _client = new HttpClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact(Skip = "Manual only: requires DEEPSEEK_API_KEY and optional network access")]
    public async Task Kernel_InvokeAsync_CanInvokeOrdersCreate()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var deepseekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrWhiteSpace(deepseekKey))
        {
            kernelBuilder.AddOpenAIChatCompletion("gpt-4o-mini", deepseekKey);
        }

        var kernel = kernelBuilder.Build();
        var ordersPlugin = new OrdersPlugin(new NullServiceInvoker());
        kernel.ImportPluginFromObject(ordersPlugin, "Orders");

        // Invoke the plugin function via kernel using SK 1.x API
        var result = await kernel.InvokeAsync("Orders", "CreateAsync", new KernelArguments
        {
            ["payloadJson"] = "{ \"orderNumber\": \"TEST123\" }"
        });

        result.Should().NotBeNull();
        var value = result.ToString();
        value.Should().NotBeNull();
    }
}
