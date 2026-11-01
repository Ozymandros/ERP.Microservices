using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Authentication;
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

    /// <summary>
    /// Initializes a new instance of the ServiceInvoker class.
    /// </summary>
    /// <param name="daprClient">The dapr Client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="jsonOptions">The json Options.</param>
    /// <param name="httpContextAccessor">The http Context Accessor.</param>
    /// <param name="enableLogging">The enable Logging.</param>
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

    /// <summary>Invokes a method on a remote Dapr-enabled service with a typed request and response.</summary>
    /// <typeparam name="TRequest">The type of the request payload.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="serviceName">The Dapr app-ID of the target service.</param>
    /// <param name="methodPath">The relative path of the method to invoke.</param>
    /// <param name="httpMethod">The HTTP method to use.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized response from the target service.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> or <paramref name="methodPath"/> is null or empty.</exception>
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

            var httpRequest = CreateRequest(serviceName, methodPath, httpMethod, request);
            ApplyCallerAuthorization(httpRequest);

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TResponse>(httpRequest, cancellationToken);
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

    /// <summary>Sends a GET request to the specified service method with a typed request and response.</summary>
    /// <typeparam name="TRequest">The type of the request payload.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="serviceName">The Dapr app-ID of the target service.</param>
    /// <param name="methodPath">The relative path of the method to invoke.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized response from the target service.</returns>
    public async Task<TResponse> GetAsync<TRequest, TResponse>(
        string serviceName,
        string methodPath,
        TRequest request,
        CancellationToken cancellationToken = default) => await InvokeAsync<TRequest, TResponse>(
            serviceName, methodPath, HttpMethod.Get, request, cancellationToken);

    /// <summary>Invokes a method on a remote Dapr-enabled service with no request body, returning a typed response.</summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="serviceName">The Dapr app-ID of the target service.</param>
    /// <param name="methodPath">The relative path of the method to invoke.</param>
    /// <param name="httpMethod">The HTTP method to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized response from the target service.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> or <paramref name="methodPath"/> is null or empty.</exception>
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

            var httpRequest = CreateRequest(serviceName, methodPath, httpMethod);
            ApplyCallerAuthorization(httpRequest);

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TResponse>(httpRequest, cancellationToken);
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

    /// <summary>
    /// Invoke asynchronously.
    /// </summary>
    /// <param name="serviceName">The service Name.</param>
    /// <param name="methodPath">The method Path.</param>
    /// <param name="httpMethod">The http Method.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
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

            var httpRequest = CreateRequest(serviceName, methodPath, httpMethod);
            ApplyCallerAuthorization(httpRequest);

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            await _daprClient.InvokeMethodAsync(httpRequest, cancellationToken);
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

    /// <summary>
    /// Creates a request.
    /// </summary>
    /// <param name="serviceName">The service Name.</param>
    /// <param name="methodPath">The method Path.</param>
    /// <param name="httpMethod">The http Method.</param>
    /// <param name="requestBody">The request Body.</param>
    /// <param name="queryParams">The query Params.</param>
    /// <returns>The result of the operation.</returns>
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

        // 1. Gestionar Query Parameters
        string finalPath = methodPath;
        if (queryParams != null && queryParams.Count > 0)
        {
            finalPath = QueryHelpers.AddQueryString(methodPath, queryParams);
        }

        // 2. Create the base request to Dapr
        // IMPORTANT: Do not pass the 'requestBody' here yet so we keep full control
        var request = _daprClient.CreateInvokeMethodRequest(httpMethod, serviceName, finalPath);

        // 3. Handle the body (ONLY if it is not GET)
        if (requestBody != null)
        {
            if (httpMethod == HttpMethod.Get)
            {
                _logger.LogWarning("Intent de passar un body en una petició GET al servei {Service}. El body serà ignorat.", serviceName);
            }
            else
            {
                var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
        }

        return request;
    }

    /// <summary>Invokes a Dapr service using a pre-built <see cref="HttpRequestMessage"/>, forwarding the caller's Bearer token if present.</summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The pre-built HTTP request message targeting the Dapr sidecar.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized response from the target service.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
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

            ApplyCallerAuthorization(request);

#pragma warning disable CS0618 // Dapr InvokeMethodAsync is obsolete but intentionally used
            var response = await _daprClient.InvokeMethodAsync<TResponse>(request, cancellationToken);
#pragma warning restore CS0618

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

    /// <summary>
    /// Copies the inbound request's Bearer token onto the outbound Dapr invoke request when present.
    /// </summary>
    private void ApplyCallerAuthorization(HttpRequestMessage request)
    {
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) is not true)
            return;

        var token = BearerTokenHelper.ExtractToken(authHeader);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
