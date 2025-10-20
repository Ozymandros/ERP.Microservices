using CommunityToolkit.Aspire.Hosting.Dapr;

public class AspireProjectBuilder
{
    private int _httpPort = 5000;
    private int _daprHttpPort = 3500;
    private int _daprGrpcPort = 45000;
    private int _metricsPort = 9090;

    private readonly IDistributedApplicationBuilder _builder;
    private readonly IResourceBuilder<SqlServerServerResource> _sqlServer;

    public AspireProjectBuilder(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerServerResource> sqlServer)
    {
        _builder = builder;
        _sqlServer = sqlServer;
    }

    public IResourceBuilder<ProjectResource> AddWebProject<T>()
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
        //var httpPort = _httpPort++;
        //var daprHttpPort = _daprHttpPort++;
        //var daprGrpcPort = _daprGrpcPort++;
        //var metricsPort = _metricsPort++;

        // Add database
        var database = _sqlServer.AddDatabase(dbName);

        // Add project
        var project = _builder.AddProject<T>(serviceResourceName);

        // Configure project
        project = project
            .WithHttpEndpoint()//httpPort
            //.WithHttpsEndpoint()
            .WithExternalHttpEndpoints()
            .WaitFor(database)
            .WithReference(database)
            .WithHttpHealthCheck(path: "/health", statusCode: 200)
            //.WithDaprSidecarOptions(options => options.AddArgument("--enable-scheduler", "false"))
            .WithDaprSidecar(new DaprSidecarOptions
            {
                //AppId = daprAppId,
                //AppPort = httpPort,
                //DaprHttpPort = daprHttpPort,
                //DaprGrpcPort = daprGrpcPort,
                //MetricsPort = metricsPort
            }).WithArgs("--enable-scheduler=false")
            .WithEnvironment("Jwt__SecretKey", _builder.Configuration["Jwt:SecretKey"])
            .WithEnvironment("Jwt__Issuer", _builder.Configuration["Jwt:Issuer"])
            .WithEnvironment("Jwt__Audience", _builder.Configuration["Jwt:Audience"]); ;

        return project;
    }

    // Optionally reset counters
    public void ResetCounters(
        int httpPort = 5000,
        int daprHttpPort = 3500,
        int daprGrpcPort = 45000,
        int metricsPort = 9090)
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
        IResourceBuilder<SqlServerServerResource> sqlServer)
    {
        return new AspireProjectBuilder(builder, sqlServer);
    }
}