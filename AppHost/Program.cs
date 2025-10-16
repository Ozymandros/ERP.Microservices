using Aspire.Hosting.Yarp.Transforms;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

// Afegir el teu microserviciAppId 
builder.AddRedis("redis").WithDaprSidecar(new DaprSidecarOptions
{
    AppId = "redis",
    //AppPort = 6380, // Port alternatiu
    DaprHttpPort = 3600,
    DaprGrpcPort = 46000,
    MetricsPort = 9100
});

// Afegir SQL Server com a contenidor
var password = builder.AddParameter("password", secret: true);
var sqlServer = builder.AddSqlServer("sqlserver", password);

var usersDb = sqlServer.AddDatabase("UsersDB");
//var notificationsDb = sqlServer.AddDatabase("NotificationsDB");

// Create builder with automatic port management
var projectBuilder = builder.CreateProjectBuilder(sqlServer);

// Add projects - ports auto-increment
var authService = projectBuilder.AddWebProject<Projects.MyApp_Auth_API>();
// Creates: BillingDB, billing-service, ports 5000, 3500, 45000, 9090

var billingService = projectBuilder.AddWebProject<Projects.MyApp_Billing_API>();
// Creates: BillingDB, billing-service, ports 5001, 3501, 45001, 9091

var inventoryService = projectBuilder.AddWebProject<Projects.MyApp_Inventory_API>();
// Creates: InventoryDB, inventory-service, ports 5002, 3502, 45002, 9092

var ordersService = projectBuilder.AddWebProject<Projects.MyApp_Orders_API>();
// Creates: OrderDB, order-service, ports 5003, 3503, 45003, 9093

var purchasingService = projectBuilder.AddWebProject<Projects.MyApp_Purchasing_API>();
// Creates: OrderDB, order-service, ports 5004, 3504, 45004, 9094

var salesService = projectBuilder.AddWebProject<Projects.MyApp_Sales_API>();
// Creates: OrderDB, order-service, ports 5005, 3505, 45005, 9095

// Configuració del Reverse Proxy (YARP)
var gateway = builder.AddYarp("gateway")
                     //.WithHttpEndpoint(5000)
                     .WithConfiguration(yarp =>
                     {
                         // Configure routes programmatically
                         yarp.AddRoute("/inventory/{**catch-all}", inventoryService)
                             .WithTransformPathRemovePrefix("/inventory");
                         yarp.AddRoute("/sales/{**catch-all}", salesService)
                             .WithTransformPathRemovePrefix("/sales");
                         yarp.AddRoute("/billing/{**catch-all}", billingService)
                             .WithTransformPathRemovePrefix("/billing");
                         yarp.AddRoute("/orders/{**catch-all}", ordersService)
                             .WithTransformPathRemovePrefix("/orders");
                         yarp.AddRoute("/purchasing/{**catch-all}", purchasingService)
                             .WithTransformPathRemovePrefix("/purchasing");
                         yarp.AddRoute("/auth/{**catch-all}", authService)
                             .WithTransformPathRemovePrefix("/auth");
                         //yarp.AddRoute("/notification/{**catch-all}", notificationService)
                         //    .WithTransformPathRemovePrefix("/notification");
                     });

builder.Build().Run();

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

        // Get current ports and increment
        var httpPort = _httpPort++;
        var daprHttpPort = _daprHttpPort++;
        var daprGrpcPort = _daprGrpcPort++;
        var metricsPort = _metricsPort++;

        // Add database
        var database = _sqlServer.AddDatabase(dbName);

        // Add project
        var project = _builder.AddProject<T>(serviceResourceName);

        // Configure project
        project = project
            .WithHttpEndpoint(httpPort)
            .WithExternalHttpEndpoints()
            .WaitFor(database)
            .WithReference(database)
            .WithDaprSidecar(new DaprSidecarOptions
            {
                AppId = daprAppId,
                AppPort = httpPort,
                DaprHttpPort = daprHttpPort,
                DaprGrpcPort = daprGrpcPort,
                MetricsPort = metricsPort
            });

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