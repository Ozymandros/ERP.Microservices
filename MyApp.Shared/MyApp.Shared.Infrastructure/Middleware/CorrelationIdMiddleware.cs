using Serilog.Context;
using MyApp.Shared.Infrastructure.Logging;

namespace MyApp.Shared.Infrastructure.Middleware;

/// <summary>
/// Middleware that automatically extracts correlation IDs from request headers
/// and pushes them to Serilog context for all subsequent operations.
/// This ensures that all logs from a single request share the same correlation ID.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdPropertyName = "CorrelationId";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from request header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Push to Serilog context for the duration of the request
        using (LogContext.PushProperty(CorrelationIdPropertyName, correlationId))
        {
            // Add additional context properties
            using (LogContext.PushProperty("Path", context.Request.Path.ToString()))
            using (LogContext.PushProperty("Method", context.Request.Method))
            {
                await _next(context);
            }
        }
    }
}

/// <summary>
/// Extension methods for registering the correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the request pipeline.
    /// Should be called early in the middleware chain, ideally before any other middleware.
    /// 
    /// Usage:
    /// <code>
    /// var app = builder.Build();
    /// app.UseCorrelationIdMiddleware();
    /// app.UseRouting();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
