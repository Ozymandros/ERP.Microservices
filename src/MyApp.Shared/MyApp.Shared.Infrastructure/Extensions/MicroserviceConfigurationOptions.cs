using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Configuration options for common microservice setup
/// </summary>
public class MicroserviceConfigurationOptions
{
    /// <summary>
    /// Service name for OpenTelemetry (defaults to assembly name if not provided)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Database connection string key (e.g., "OrdersDb", "SalesDb", "inventorydb")
    /// Must be provided if DbContextType is set. Connection string keys are case-insensitive.
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
    /// 
    /// IMPORTANT: When enabled, you MUST call builder.AddRedisDistributedCache("cache") BEFORE AddServiceDefaults.
    /// This is because AddRedisDistributedCache is an Aspire extension method that requires the Redis resource
    /// reference from the AppHost project. The connection name defaults to "cache" but can be overridden via RedisConnectionName.
    /// 
    /// Example:
    ///   builder.AddRedisDistributedCache("cache");  // Must be called FIRST (Aspire extension)
    ///   builder.AddServiceDefaults(...);             // Then configure service defaults
    /// </summary>
    public bool EnableRedisCache { get; set; } = true;

    /// <summary>
    /// Enable AutoMapper (default: true)
    /// </summary>
    public bool EnableAutoMapper { get; set; } = true;

    /// <summary>
    /// AutoMapper profile assembly.
    /// Must be provided if EnableAutoMapper is true (no auto-detection to avoid reflection issues).
    /// Example: typeof(MyApp.Orders.Application.Mapping.OrderProfile).Assembly
    /// </summary>
    public System.Reflection.Assembly? AutoMapperAssembly { get; set; }

    /// <summary>
    /// Action to configure service-specific dependencies (repositories, services, etc.)
    /// </summary>
    public Action<IServiceCollection>? ConfigureServiceDependencies { get; set; }

    /// <summary>
    /// Redis connection name for Aspire (default: "cache")
    /// </summary>
    public string RedisConnectionName { get; set; } = "cache";
}
