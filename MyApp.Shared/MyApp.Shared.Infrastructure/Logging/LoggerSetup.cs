using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MyApp.Shared.Infrastructure.Logging;

/// <summary>
/// Centralized Serilog logger configuration for the entire application.
/// Provides structured logging with multiple sinks (Console, File).
/// Automatically enriches logs with context information (MachineName, ThreadId, EnvironmentName).
/// 
/// Usage in Program.cs:
/// <code>
/// using Serilog;
/// using MyApp.Shared.Infrastructure.Logging;
/// 
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// // Configure Serilog before building
/// builder.Host.UseSerilog((context, config) =>
/// {
///     LoggerSetup.Configure(config, context.Configuration, context.HostingEnvironment);
/// });
/// </code>
/// </summary>
public static class LoggerSetup
{
    /// <summary>
    /// Configures Serilog with centralized settings for all microservices.
    /// </summary>
    public static void Configure(
        LoggerConfiguration config,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        config
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("EnvironmentName", environment.EnvironmentName)
            .Enrich.WithProperty("ApplicationName", environment.ApplicationName ?? "Unknown");

        // Apply custom log levels from Logging section
        ApplyLogLevelConfiguration(config, configuration);

        // Apply sinks configuration (Console and File)
        ApplySinkConfiguration(config, configuration);
    }

    private static void ApplyLogLevelConfiguration(
        LoggerConfiguration config,
        IConfiguration configuration)
    {
        var logging = configuration.GetSection("Logging");
        if (!logging.Exists())
            return;

        // Set default minimum level
        var defaultLevel = logging.GetValue<string>("LogLevel:Default");
        if (!string.IsNullOrEmpty(defaultLevel) && 
            Enum.TryParse<LogEventLevel>(defaultLevel, out var level))
        {
            config.MinimumLevel.Is(level);
        }

        // Override specific namespaces
        var logLevels = logging.GetSection("LogLevel");
        foreach (var child in logLevels.GetChildren())
        {
            if (Enum.TryParse<LogEventLevel>(child.Value, out var nsLevel))
            {
                config.MinimumLevel.Override(child.Key, nsLevel);
            }
        }
    }

    private static void ApplySinkConfiguration(
        LoggerConfiguration config,
        IConfiguration configuration)
    {
        var serilog = configuration.GetSection("Serilog");

        // Console sink configuration
        if (serilog.GetValue("Sinks:Console:Enabled", true))
        {
            var format = serilog.GetValue<string>("Sinks:Console:Format", "compact");
            var template = serilog.GetValue<string>("Sinks:Console:OutputTemplate", "");

            if (string.IsNullOrEmpty(template))
            {
                template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            }

            if (format == "json")
            {
                config.WriteTo.Console(
                    formatter: new Serilog.Formatting.Json.JsonFormatter());
            }
            else
            {
                config.WriteTo.Console(outputTemplate: template);
            }
        }

        // File sink configuration
        if (serilog.GetValue("Sinks:File:Enabled", true))
        {
            var path = serilog.GetValue<string>("Sinks:File:Path") ?? "logs/app-.log";
            var template = serilog.GetValue<string>("Sinks:File:OutputTemplate", "");
            var retainedCount = serilog.GetValue("Sinks:File:RetainedFileCountLimit", 30);

            if (string.IsNullOrEmpty(template))
            {
                template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
            }

            // Ensure log directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            config.WriteTo.File(
                path: path,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedCount,
                outputTemplate: template);
        }
    }

    /// <summary>
    /// Gracefully closes and flushes all pending log entries.
    /// Call this during application shutdown.
    /// </summary>
    public static async Task CloseAndFlushAsync()
    {
        try
        {
            await Log.CloseAndFlushAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to close and flush Serilog");
        }
    }
}
