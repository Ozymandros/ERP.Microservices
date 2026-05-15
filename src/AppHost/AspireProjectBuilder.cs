using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;

/// <summary>
/// Provides Aspire Project Builder functionality.
/// </summary>
public class AspireProjectBuilder
{
    private int _httpPort = 6000;
    private int _daprHttpPort = 3500;
    private int _daprGrpcPort = 45000;
    private int _metricsPort = 9090;

    private readonly IDistributedApplicationBuilder _builder;
    private readonly IResourceBuilder<SqlServerServerResource>? _sqlServer;
    private readonly IResourceBuilder<AzureSqlServerResource>? _sqlAzureServer;

    private readonly string? _keyVault;

    /// <summary>
    /// Aspire Project Builder constructor. Initializes the builder with optional SQL Server and Azure SQL Server resources, and an optional Key Vault reference.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="sqlServer"></param>
    /// <param name="sqlAzureServer"></param>
    /// <param name="keyVault"></param>
    public AspireProjectBuilder(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerServerResource>? sqlServer = null,
        IResourceBuilder<AzureSqlServerResource>? sqlAzureServer = null,
        string? keyVault = null
        )
    {
        _builder = builder;
        _sqlServer = sqlServer;
        _sqlAzureServer = sqlAzureServer;
        _keyVault = keyVault;
    }

    /// <summary>
    /// Add Web Project. Creates a new project with the specified type and adds it to the builder. The project is configured with Dapr, environment variables, and optional Redis and Application Insights resources.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="redis"></param>
    /// <param name="origin"></param>
    /// <param name="isDeployment"></param>
    /// <param name="applicationInsights"></param>
    /// <param name="pubSub"></param>
    /// <param name="stateStore"></param>
    /// <param name="hasDatabase"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IResourceBuilder<ProjectResource> AddWebProject<T>(
        IResourceBuilder<RedisResource>? redis = null,
        string? origin = null,
        bool isDeployment = false,
        IResourceBuilder<AzureApplicationInsightsResource>? applicationInsights = null,
        IResourceBuilder<IDaprComponentResource>? pubSub = null,
        IResourceBuilder<IDaprComponentResource>? stateStore = null,
        bool hasDatabase = true)
        where T : IProjectMetadata, new()
    {
        // Extract service name from type name
        // Example: "MyApp_Billing_API" -> "Billing"
        var typeName = typeof(T).Name;
        var parts = typeName.Split('_');

        if (parts.Length < 2)
        {
            throw new ArgumentException(
                $"Type name '{typeName}' must contain at least one underscore separator. " +
                "Expected format: Prefix_ServiceName_Suffix");
        }

        // Get middle word (index 1)
        var serviceName = parts[1];
        var serviceNameLower = serviceName.ToLower();

        // Create database and service names
        var dbName = $"{serviceName}DB";
        var serviceResourceName = $"{serviceNameLower}-service";
        var daprAppId = $"{serviceNameLower}-service";

        // Get current ports and increment (thread-safe)
        var httpPort = Interlocked.Increment(ref _httpPort);
        var aspNetCoreUrls = "http://127.0.0.1:" + httpPort;
        //var daprHttpPort = _daprHttpPort++;
        //var daprGrpcPort = _daprGrpcPort++;
        //var metricsPort = _metricsPort++;

        // Add project
        var project = _builder.AddProject<T>(serviceResourceName);

        var sidecarOptions = new DaprSidecarOptions
        {
            AppId = daprAppId,
            AppPort = httpPort,
            // Note: Placement and Scheduler connection errors are harmless warnings.
            // - Placement (port 6050): Only needed for Dapr Actors (we don't use actors)
            // - Scheduler (port 6060): Only needed for scheduled jobs/workflows (we don't use)
            // PubSub and State Store work perfectly without these services.
        };
        // Configure project
        // Note: Aspire uses its own integrated Dapr runtime version (currently 1.15.x)
        // The Dapr CLI installation in DevContainer does not affect the sidecar version
        // Scheduler and Placement connection errors are harmless warnings (not used)
        project = project
            .WithDaprSidecar(CreateSidecarMapping(sidecarOptions, pubSub, stateStore))
            .WithEnvironment("Jwt__SecretKey", _builder.Configuration["Jwt:SecretKey"])
            .WithEnvironment("Jwt__Issuer", _builder.Configuration["Jwt:Issuer"])
            .WithEnvironment("Jwt__Audience", _builder.Configuration["Jwt:Audience"])
            .WithEnvironment("ASPNETCORE_URLS", aspNetCoreUrls)
            .WithEnvironment("DOTNET_LAUNCH_PROFILE", string.Empty)
            .WithEnvironment("ALLOWED_ORIGINS", origin)
            // OpenTelemetry configuration for Serilog
            .WithEnvironment("OTEL_SERVICE_NAME", serviceName)
            .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
            .PublishAsDockerFile();

        var database = hasDatabase ? _sqlServer?.AddDatabase(dbName) : null;
        if (database is not null)
        {
            project = project.WaitFor(database);
            project = project.WithReference(database);
        }

        //    project = project.WithHttpEndpoint(port: httpPort, name: "http") // use default name
        //.WithHttpHealthCheck(path: "/health", statusCode: 200);

        // Application Insights
        if (applicationInsights is not null)
        {
            project = project
                .WaitFor(applicationInsights)
                .WithReference(applicationInsights);
        }

        // Redis Cache
        if (redis is not null)
            project
                //.WaitFor(store)
                //.WithReference(store)
                .WaitFor(redis)
                .WithReference(redis);

        return project;

        static Action<IResourceBuilder<IDaprSidecarResource>> CreateSidecarMapping(
            DaprSidecarOptions sidecarOptions,
            IResourceBuilder<IDaprComponentResource>? pubSub,
            IResourceBuilder<IDaprComponentResource>? stateStore)
        {
            if (pubSub is not null)
                if (stateStore is not null)
                    return sidecar => sidecar.WithOptions(sidecarOptions).WithReference(stateStore).WithReference(pubSub);
                else
                    return sidecar => sidecar.WithOptions(sidecarOptions).WithReference(pubSub);

            return sidecar => sidecar.WithOptions(sidecarOptions);
        }
    }

    // Optionally reset counters
    /// <summary>Resets the port counters to the specified values.</summary>
    /// <param name="httpPort">The starting HTTP port.</param>
    /// <param name="daprHttpPort">The starting Dapr HTTP port.</param>
    /// <param name="daprGrpcPort">The starting Dapr gRPC port.</param>
    /// <param name="metricsPort">The starting metrics port.</param>
    public void ResetCounters(
        int httpPort = 6001,
        int daprHttpPort = 3501,
        int daprGrpcPort = 46001,
        int metricsPort = 9091)
    {
        _httpPort = httpPort;
        _daprHttpPort = daprHttpPort;
        _daprGrpcPort = daprGrpcPort;
        _metricsPort = metricsPort;
    }
}

// Extension method for cleaner usage

/// <summary>
/// Provides Aspire Project Builder Extensions functionality. Provides a static method for creating an AspireProjectBuilder instance.
/// </summary>
public static class AspireProjectBuilderExtensions
{
    /// <summary>
    /// Create Project Builder. Creates an AspireProjectBuilder instance with the specified resources and Key Vault reference.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="sqlServer"></param>
    /// <param name="sqlAzure"></param>
    /// <param name="keyVault"></param>
    /// <returns></returns>
    public static AspireProjectBuilder CreateProjectBuilder(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerServerResource>? sqlServer = null,
        IResourceBuilder<AzureSqlServerResource>? sqlAzure = null,
        string? keyVault = null)
    {
        return new AspireProjectBuilder(builder, sqlServer, sqlAzure, keyVault);
    }
}