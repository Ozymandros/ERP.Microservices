using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Messaging;
using System.Net.Http.Headers;
using System.Text.Json;

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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServiceInvoker(
        DaprClient daprClient,
        ILogger<ServiceInvoker> logger,
        IOptions<JsonSerializerOptions> jsonOptions,
        IHttpContextAccessor httpContextAccessor,
        bool enableLogging = true)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        _daprClient = daprClient;
        _logger = logger;
        _enableLogging = enableLogging;
        _jsonOptions = jsonOptions.Value ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        _httpContextAccessor = httpContextAccessor;
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

        ArgumentNullException.ThrowIfNull(httpMethod);

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TRequest, TResponse>(
                httpMethod,
                serviceName,
                methodPath,
                request,
                cancellationToken);
#pragma warning restore CS0618

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

    public async Task<TResponse> GetAsync<TRequest, TResponse>(
        string serviceName,
        string methodPath,
        TRequest request,
        CancellationToken cancellationToken = default) => await InvokeAsync<TRequest, TResponse>(
            serviceName, methodPath, HttpMethod.Get, request, cancellationToken);

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

        ArgumentNullException.ThrowIfNull(httpMethod);

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TResponse>(
                httpMethod,
                serviceName,
                methodPath,
                cancellationToken);
#pragma warning restore CS0618

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

        ArgumentNullException.ThrowIfNull(httpMethod);

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service: {@Request}",
                    new { ServiceName = serviceName, MethodPath = methodPath, HttpMethod = httpMethod.Method });
            }

            cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            await _daprClient.InvokeMethodAsync(
                httpMethod,
                serviceName,
                methodPath,
                cancellationToken);
#pragma warning restore CS0618

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
        Dictionary<string, string?>? queryParams = null)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(methodPath))
            throw new ArgumentException("Method path cannot be null or empty", nameof(methodPath));

        ArgumentNullException.ThrowIfNull(httpMethod);

        HttpRequestMessage request;

        // 1. Gestionar Query Parameters
        string finalPath = methodPath;
        if (queryParams != null && queryParams.Count > 0)
        {
            // El teu loop de validaci� de claus/valors �s correcte
            finalPath = QueryHelpers.AddQueryString(methodPath, queryParams);
        }

        // 2. Crear la petici� base a Dapr
        // IMPORTANT: No passem el 'requestBody' aqu� encara per tenir control total
        request = _daprClient.CreateInvokeMethodRequest(httpMethod, serviceName, finalPath);

        // 3. Gestionar el Body (NOM�S si no �s GET)
        if (requestBody != null)
        {
            if (httpMethod == HttpMethod.Get)
            {
                // Opcional: Pots llan�ar una excepci� o simplement ignorar-lo i avisar
                _logger.LogWarning("Intent de passar un body en una petici� GET al servei {Service}. El body ser� ignorat.", serviceName);
            }
            else
            {
                var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
        }

        // ... (Logging final igual)

        return request;
    }

    public async Task<TResponse> InvokeAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (_enableLogging)
            {
                _logger.LogInformation(
                    "Invoking service with custom request: {@Request}",
                    new { Method = request.Method?.ToString(), RequestUri = request.RequestUri?.ToString() });
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is true)
            {
                var token = authHeader.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TResponse>(request, cancellationToken);

            if (_enableLogging)
            {
                _logger.LogTrace("Successfully invoked service with custom request");
#pragma warning restore CS0618
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
