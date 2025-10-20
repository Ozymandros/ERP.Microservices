using Serilog;
using Serilog.Context;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace MyApp.Shared.Infrastructure.Logging;

/// <summary>
/// Extension methods for structured logging with correlation IDs and context enrichment.
/// </summary>
public static class SerilogExtensions
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdPropertyName = "CorrelationId";

    /// <summary>
    /// Pushes correlation ID to the Serilog context for the current scope.
    /// Should be used in middleware or at the start of a request/operation.
    /// </summary>
    public static IDisposable PushCorrelationId(string? correlationId = null)
    {
        var id = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        return LogContext.PushProperty(CorrelationIdPropertyName, id);
    }

    /// <summary>
    /// Pushes correlation ID from HTTP context to Serilog context.
    /// Retrieves from X-Correlation-ID header or generates a new GUID.
    /// </summary>
    public static IDisposable PushCorrelationIdFromHttpContext(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString();

        // Ensure correlation ID is returned in response
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        return PushCorrelationId(correlationId);
    }

    /// <summary>
    /// Logs an operation with timing information and structured context.
    /// Usage:
    /// <code>
    /// using var _ = logger.LogOperation("ProcessOrder", new { orderId = 123, userId = 456 });
    /// // Your operation code here
    /// </code>
    /// </summary>
    public static IDisposable LogOperation(
        this ILogger logger,
        string operationName,
        object? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.Information("Operation started: {OperationName}", operationName);

        if (context != null)
        {
            LogContext.PushProperty("OperationContext", context);
        }

        return new OperationStopwatch(logger, operationName, stopwatch);
    }

    /// <summary>
    /// Logs structured exception with additional context properties.
    /// </summary>
    public static void LogExceptionWithContext(
        this ILogger logger,
        Exception exception,
        string message,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            logger.Error(exception, message);
        }
    }

    /// <summary>
    /// Logs a debug message with structured context.
    /// </summary>
    public static void LogDebugWithContext(
        this ILogger logger,
        string message,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            logger.Debug(message);
        }
    }

    /// <summary>
    /// Logs an information message with structured context.
    /// </summary>
    public static void LogInfoWithContext(
        this ILogger logger,
        string message,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            logger.Information(message);
        }
    }

    /// <summary>
    /// Logs a warning message with structured context.
    /// </summary>
    public static void LogWarningWithContext(
        this ILogger logger,
        string message,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            logger.Warning(message);
        }
    }

    /// <summary>
    /// Logs an error message with structured context and optional exception.
    /// </summary>
    public static void LogErrorWithContext(
        this ILogger logger,
        string message,
        Exception? exception = null,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            if (exception != null)
            {
                logger.Error(exception, message);
            }
            else
            {
                logger.Error(message);
            }
        }
    }

    /// <summary>
    /// Logs a fatal error with structured context and optional exception.
    /// </summary>
    public static void LogFatalWithContext(
        this ILogger logger,
        string message,
        Exception? exception = null,
        params (string key, object value)[] contextProperties)
    {
        using (LogContext.PushProperties(contextProperties.Select(x => (x.key, x.value as object))))
        {
            if (exception != null)
            {
                logger.Fatal(exception, message);
            }
            else
            {
                logger.Fatal(message);
            }
        }
    }

    /// <summary>
    /// Internal helper class for timing operations.
    /// </summary>
    private class OperationStopwatch : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationStopwatch(ILogger logger, string operationName, Stopwatch stopwatch)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = stopwatch;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _stopwatch.Stop();
            _logger.Information(
                "Operation completed: {OperationName} - Duration: {DurationMs}ms",
                _operationName,
                _stopwatch.ElapsedMilliseconds);

            _disposed = true;
        }
    }
}
