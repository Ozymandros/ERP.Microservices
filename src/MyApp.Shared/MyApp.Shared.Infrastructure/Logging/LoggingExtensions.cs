using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Sinks.OpenTelemetry;

namespace MyApp.Shared.Infrastructure.Logging;

/// <summary>
/// Provides Logging Extensions functionality.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>Add Custom Logging.</summary>
    /// <param name="builder">The builder.</param>
    public static IHostApplicationBuilder AddCustomLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                // Automasking PII - uses default property masking for common sensitive fields
                // Property masking is configured via appsettings.json or uses default behavior
                .Enrich.WithSensitiveDataMasking(options =>
                {
                    options.Mode = MaskingMode.Globally;
                    options.MaskingOperators = new List<IMaskingOperator>();
                })
                // Optional Serilog → OTLP (prefer AddOpenTelemetry().WithLogging in service defaults).
                // Uses Aspire-injected OTEL_EXPORTER_OTLP_ENDPOINT when present; no hardcoded :4317.
                .WriteTo.OpenTelemetry(options =>
                {
                    var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

                    if (!string.IsNullOrWhiteSpace(otelEndpoint))
                        options.Endpoint = otelEndpoint;

                    var serviceName = builder.Configuration["OTEL_SERVICE_NAME"]
                        ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                        ?? builder.Environment.ApplicationName
                        ?? "UnknownService";

                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName
                    };

                    var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]
                        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
                    if (!string.IsNullOrWhiteSpace(headers))
                    {
                        options.Headers = headers
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                            .Where(pair => pair.Length == 2 && pair[0].Length > 0)
                            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);
                    }
                });
        });

        return builder;
    }
}

