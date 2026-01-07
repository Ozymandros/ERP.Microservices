using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Configuration options for common microservice setup
/// </summary>
public class MicroserviceConfigurationOptions
{
    /// <summary>
    /// Service name for OpenTelemetry (defaults to assembly name)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Database connection string key (e.g., "OrdersDb", "SalesDb")
    /// </summary>
    public string? ConnectionStringKey { get; set; }

    /// <summary>
    /// Database context type for migrations (null to skip migrations)
    /// </summary>
    public Type? DbContextType { get; set; }

    /// <summary>
    /// Enable JWT authentication (default: true)
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// Enable health checks (default: true)
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable OpenTelemetry (default: true)
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// Enable DAPR client (default: true)
    /// </summary>
    public bool EnableDapr { get; set; } = true;

    /// <summary>
    /// Enable Redis distributed cache (default: true)
    /// </summary>
    public bool EnableRedisCache { get; set; } = true;

    /// <summary>
    /// Enable AutoMapper (default: true)
    /// </summary>
    public bool EnableAutoMapper { get; set; } = true;

    /// <summary>
    /// AutoMapper profile assembly (if null, uses calling assembly)
    /// </summary>
    public System.Reflection.Assembly? AutoMapperAssembly { get; set; }

    /// <summary>
    /// Action to configure service-specific dependencies (repositories, services, etc.)
    /// </summary>
    public Action<IServiceCollection>? ConfigureServiceDependencies { get; set; }
}
