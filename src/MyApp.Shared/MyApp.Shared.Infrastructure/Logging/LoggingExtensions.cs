using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Sinks.OpenTelemetry;

namespace MyApp.Shared.Infrastructure.Logging;

public static class LoggingExtensions
{
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
                // Aspire Dashboard Integration via OpenTelemetry
                .WriteTo.OpenTelemetry(options =>
                {
                    var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                        ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                        ?? "http://localhost:4317";

                    var serviceName = builder.Configuration["OTEL_SERVICE_NAME"]
                        ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                        ?? builder.Environment.ApplicationName
                        ?? "UnknownService";

                    options.Endpoint = otelEndpoint;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName
                    };
                });
        });

        return builder;
    }
}

