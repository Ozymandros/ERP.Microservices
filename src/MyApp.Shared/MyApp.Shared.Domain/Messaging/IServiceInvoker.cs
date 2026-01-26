namespace MyApp.Shared.Domain.Messaging;

/// <summary>
/// Abstraction for invoking remote service methods
/// </summary>
public interface IServiceInvoker
{
    /// <summary>
    /// Invokes a remote service method with request and response
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="serviceName">The name of the target service</param>
    /// <param name="methodPath">The method path (e.g., "api/products")</param>
    /// <param name="httpMethod">The HTTP method to use</param>
    /// <param name="request">The request payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the service</returns>
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        TRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a remote service method with only a response
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="serviceName">The name of the target service</param>
    /// <param name="methodPath">The method path (e.g., "api/products")</param>
    /// <param name="httpMethod">The HTTP method to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the service</returns>
    Task<TResponse> InvokeAsync<TResponse>(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a remote service method without expecting a response
    /// </summary>
    /// <param name="serviceName">The name of the target service</param>
    /// <param name="methodPath">The method path (e.g., "api/products")</param>
    /// <param name="httpMethod">The HTTP method to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InvokeAsync(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an HTTP request for invoking a remote service method
    /// </summary>
    /// <param name="serviceName">The name of the target service</param>
    /// <param name="methodPath">The method path (e.g., "api/products")</param>
    /// <param name="httpMethod">The HTTP method to use</param>
    /// <param name="requestBody">Optional request body</param>
    /// <param name="queryParams">Optional query parameters</param>
    /// <returns>An HTTP request message that can be customized before sending</returns>
    HttpRequestMessage CreateRequest(
        string serviceName,
        string methodPath,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string?>? queryParams = null);

    /// <summary>
    /// Invokes a remote service method using a pre-configured HttpRequestMessage (useful for custom headers, etc.)
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The HTTP request message (typically created by CreateRequest and then customized)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the service</returns>
    Task<TResponse> InvokeAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);
}
