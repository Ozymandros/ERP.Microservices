using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Shared.Infrastructure.Messaging;

/// <summary>
/// Dapr-based implementation of IServiceInvoker
/// </summary>
public class ServiceInvoker : IServiceInvoker
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<ServiceInvoker> _logger;
    private readonly bool _enableLogging;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceInvoker(
        DaprClient daprClient,
        ILogger<ServiceInvoker> logger,
        IOptions<JsonSerializerOptions> jsonOptions,
        bool enableLogging = true)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enableLogging = enableLogging;
        _jsonOptions = jsonOptions?.Value ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(methodPath))
            throw new ArgumentException("Method path cannot be null or empty", nameof(methodPath));

        if (httpMethod == null)
            throw new ArgumentNullException(nameof(httpMethod));

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

            var response = await _daprClient.InvokeMethodAsync<TRequest, TResponse>(
                httpMethod,
                serviceName,
                methodPath,
                request,
                cancellationToken);

            if (_enableLogging)
            {
                _logger.LogTrace(
                    "Successfully invoked service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invoke service: {@Request}",
                new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            throw;
        }
    }

    public async Task<TResponse> InvokeAsync<TResponse>(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(methodPath))
            throw new ArgumentException("Method path cannot be null or empty", nameof(methodPath));

        if (httpMethod == null)
            throw new ArgumentNullException(nameof(httpMethod));

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

            var response = await _daprClient.InvokeMethodAsync<TResponse>(
                httpMethod,
                serviceName,
                methodPath,
                cancellationToken);

            if (_enableLogging)
            {
                _logger.LogTrace(
                    "Successfully invoked service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invoke service: {@Request}",
                new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            throw;
        }
    }

    public async Task InvokeAsync(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(methodPath))
            throw new ArgumentException("Method path cannot be null or empty", nameof(methodPath));

        if (httpMethod == null)
            throw new ArgumentNullException(nameof(httpMethod));

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _daprClient.InvokeMethodAsync(
                httpMethod,
                serviceName,
                methodPath,
                cancellationToken);

            if (_enableLogging)
            {
                _logger.LogTrace(
                    "Successfully invoked service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invoke service: {@Request}",
                new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            throw;
        }
    }

    public HttpRequestMessage CreateRequest(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? queryParams = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(methodPath))
            throw new ArgumentException("Method path cannot be null or empty", nameof(methodPath));

        if (httpMethod == null)
            throw new ArgumentNullException(nameof(httpMethod));

        HttpRequestMessage request;
        
        if (queryParams != null && queryParams.Count > 0)
        {
            // Validate query params before passing to DaprClient
            foreach (var kvp in queryParams)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    throw new ArgumentException("Query parameter keys cannot be null or empty.", nameof(queryParams));
                if (kvp.Value == null)
                    throw new ArgumentException($"Query parameter value for key '{kvp.Key}' cannot be null.", nameof(queryParams));
            }
            request = _daprClient.CreateInvokeMethodRequest(httpMethod, serviceName, methodPath, queryParams);
        }
        else
        {
            request = _daprClient.CreateInvokeMethodRequest(httpMethod, serviceName, methodPath);
        }

        // If requestBody is provided, serialize it to JSON using the same options as DaprClient
        // This ensures consistency between manual serialization and DaprClient's serialization
        if (requestBody != null)
        {
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        if (_enableLogging)
        {
            _logger.LogDebug(
                "Created request for service '{ServiceName}' method '{MethodPath}' with {HttpMethod}",
                serviceName,
                methodPath,
                httpMethod.Method);
        }

        return request;
    }

    public async Task<TResponse> InvokeAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service with custom request: {@Request}",
                    new { Method = request.Method?.ToString(), RequestUri = request.RequestUri?.ToString() });
            }

            cancellationToken.ThrowIfCancellationRequested();

            var response = await _daprClient.InvokeMethodAsync<TResponse>(request, cancellationToken);

            if (_enableLogging)
            {
                _logger.LogTrace("Successfully invoked service with custom request");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invoke service with custom request: {@Request}",
                new { Method = request.Method?.ToString(), RequestUri = request.RequestUri?.ToString() });
            throw;
        }
    }
}
