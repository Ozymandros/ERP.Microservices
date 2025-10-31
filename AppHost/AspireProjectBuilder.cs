using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;

public class AspireProjectBuilder
{
    private int _httpPort = 5001;
    private int _daprHttpPort = 3501;
    private int _daprGrpcPort = 45001;
    private int _metricsPort = 9091;

    private readonly IDistributedApplicationBuilder _builder;
    private readonly IResourceBuilder<SqlServerServerResource>? _sqlServer;
    private readonly IResourceBuilder<AzureSqlServerResource>? _sqlAzureServer;

    private readonly string? _keyVault;

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

    public IResourceBuilder<ProjectResource> AddWebProject<T>(
        IResourceBuilder<RedisResource>? redis = null,
        string? origin = null,
        bool isDeployment = false,
        IResourceBuilder<AzureApplicationInsightsResource>? applicationInsights = null)
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

        // Get current ports and increment (optional)
        var httpPort = _httpPort++;
        //var daprHttpPort = _daprHttpPort++;
        //var daprGrpcPort = _daprGrpcPort++;
        //var metricsPort = _metricsPort++;

        // Add project
        var project = _builder.AddProject<T>(serviceResourceName);

        // Configure project
        project = project
            //.WithHttpEndpoint(80)
            //.WithHttpsEndpoint()
            //.WithExternalHttpEndpoints()
            //.WithDaprSidecarOptions(options => options.AddArgument("--enable-scheduler", "false"))
            .WithDaprSidecar(new DaprSidecarOptions
            {
                AppId = daprAppId,
                AppPort = httpPort,
                //DaprHttpPort = daprHttpPort,
                //DaprGrpcPort = daprGrpcPort,
                //MetricsPort = metricsPort
            }).WithArgs("--enable-scheduler=false")
            .WithEnvironment("Jwt__SecretKey", _builder.Configuration["Jwt:SecretKey"])
            .WithEnvironment("Jwt__Issuer", _builder.Configuration["Jwt:Issuer"])
            .WithEnvironment("Jwt__Audience", _builder.Configuration["Jwt:Audience"])
            .WithEnvironment("FRONTEND_ORIGIN", origin)
            .PublishAsDockerFile();

            var database = _sqlServer?.AddDatabase(dbName); ;
            if (database is not null)
            {
                project = project.WaitFor(database);
                project = project.WithReference(database);
            }
            // Local: exposem ports �nics per al dashboard
            project = project.WithHttpEndpoint(httpPort)
            .WithHttpHealthCheck(path: "/health", statusCode: 200);

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
    }

    // Optionally reset counters
    public void ResetCounters(
        int httpPort = 5001,
        int daprHttpPort = 3501,
        int daprGrpcPort = 45001,
        int metricsPort = 9091)
    {
        _httpPort = httpPort;
        _daprHttpPort = daprHttpPort;
        _daprGrpcPort = daprGrpcPort;
        _metricsPort = metricsPort;
    }
}

// Extension method for cleaner usage
public static class AspireProjectBuilderExtensions
{
    public static AspireProjectBuilder CreateProjectBuilder(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerServerResource>? sqlServer = null,
        IResourceBuilder<AzureSqlServerResource>? sqlAzure = null,
        string? keyVault = null)
    {
        return new AspireProjectBuilder(builder, sqlServer, sqlAzure, keyVault);
    }
}