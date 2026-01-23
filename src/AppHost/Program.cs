using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;

var isDeployment =
    args.Contains("--publisher") || // when azd generates manifests
    Environment.GetEnvironmentVariable("IS_DEPLOYMENT") == "true";

var builder = DistributedApplication.CreateBuilder(args).AddDapr();

var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub("pubsub");

var analyticsWorkspace = isDeployment ? builder
    .AddAzureLogAnalyticsWorkspace("MyApp-LogAnalyticsWorkspace") : null;
var applicationInsights = isDeployment ? builder
    .AddAzureApplicationInsights("MyApp-ApplicationInsights")
    .WithLogAnalyticsWorkspace(analyticsWorkspace!) : null;

builder.Services.AddHealthChecks();
//var store = builder.AddDaprStateStore("cache", new());
// Add the Redis container

// 1. Define the Redis host. Give it a name without conflicts.
// The name doesn't matter here, as the Component will define it.
var redis = builder.AddRedis("cache")
    //.WithArgs("redis-server", "--save", "", "--appendonly", "no", "--protected-mode", "no")
    .WithRedisCommander()
    .WithRedisInsight()
    .WithDataVolume("redis-cache");

// Create builder with automatic port management
AspireProjectBuilder projectBuilder;

// Add SQL Server as a container
var password = builder.AddParameter("password", secret: true, value: "Your_strong_(!)Password123");
if (isDeployment)
{
    var sqlServer = builder.AddAzureSqlServer("myapp-sqlserver");
    projectBuilder = builder.CreateProjectBuilder(null, sqlServer);
}
else
{
    var sqlServer = builder.AddSqlServer("myapp-sqlserver", password, 1455)
        .WithLifetime(ContainerLifetime.Persistent) // restarts periodically for development environments
        .WithDataVolume("sqlserver-data"); // named volume → persists between restarts
    projectBuilder = builder.CreateProjectBuilder(sqlServer);
}

var origin = builder.Configuration["Parameters:FrontendOrigin"];

// Get JWT configuration from appsettings.json or use defaults
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "una_clau_molt_llarga_i_super_ultra_secreta_01234566789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MyApp.Auth";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MyApp.All";

// Add projects - ports auto-increment
var authService = projectBuilder.AddWebProject<Projects.MyApp_Auth_API>(redis, origin, isDeployment, applicationInsights);
// Creates: BillingDB, billing-service, ports 5000, 3500, 45000, 9090

var billingService = projectBuilder.AddWebProject<Projects.MyApp_Billing_API>(redis, origin, isDeployment, applicationInsights);
// Creates: BillingDB, billing-service, ports 5001, 3501, 45001, 9091

var inventoryService = projectBuilder.AddWebProject<Projects.MyApp_Inventory_API>(redis, origin, isDeployment, applicationInsights);
// Creates: InventoryDB, inventory-service, ports 5002, 3502, 45002, 9092

var ordersService = projectBuilder.AddWebProject<Projects.MyApp_Orders_API>(redis, origin, isDeployment, applicationInsights);
// Creates: OrderDB, order-service, ports 5003, 3503, 45003, 9093

var purchasingService = projectBuilder.AddWebProject<Projects.MyApp_Purchasing_API>(redis, origin, isDeployment, applicationInsights);
// Creates: OrderDB, order-service, ports 5004, 3504, 45004, 9094

var salesService = projectBuilder.AddWebProject<Projects.MyApp_Sales_API>(redis, origin, isDeployment, applicationInsights);
// Creates: OrderDB, order-service, ports 5005, 3505, 45005, 9095

// Local Development: Reverse Proxy (YARP)
// Alternative: YARP (without /Scalar service)
/*var gateway = builder.AddYarp("gateway")
    .WaitFor(authService)
    .WaitFor(billingService)
    .WaitFor(inventoryService)
    .WaitFor(ordersService)
    .WaitFor(purchasingService)
    .WaitFor(salesService)
                     .WithHostPort(5000)
                     .WithExternalHttpEndpoints()
                     .WithConfiguration(yarp =>
                     {
                         // Configure routes programmatically
                         yarp.AddRoute("/api/auth/{**catch-all}", authService);
                         yarp.AddRoute("/api/permissions/{**catch-all}", authService);
                         yarp.AddRoute("/api/users/{**catch-all}", authService);
                         yarp.AddRoute("/api/roles/{**catch-all}", authService);
                         yarp.AddRoute("/api/billing/{**catch-all}", billingService);
                         yarp.AddRoute("/api/inventory/{**catch-all}", inventoryService);
                         yarp.AddRoute("/api/orders/{**catch-all}", ordersService);
                         yarp.AddRoute("/api/purchasing/{**catch-all}", purchasingService);
                         yarp.AddRoute("/api/sales/{**catch-all}", salesService);
                         //yarp.AddRoute("/notification/{**catch-all}", notificationService)
                         //    .WithTransformPathRemovePrefix("/notification");
                     });*/

// Alternative: ErpApiGateway with Ocelot (production)
// Note: WithExternalHttpEndpoints() will expose the HTTP endpoint from launchSettings.json (port 5000)
var gateway = builder.AddProject<Projects.ErpApiGateway>("gateway")
    .WaitFor(authService)
    .WaitFor(billingService)
    .WaitFor(inventoryService)
    .WaitFor(ordersService)
    .WaitFor(purchasingService)
    .WaitFor(salesService)
    .WithExternalHttpEndpoints() // Exposes HTTP endpoint from launchSettings.json (port 5000)
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("OCELOT_ENVIRONMENT", "Development")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "gateway",
        AppPort = 5000 // Must match launchSettings.json port
    });

if (isDeployment)
{
    gateway.WithEnvironment("OCELOT_ENVIRONMENT", "Production");
}

if (applicationInsights is not null)
{
    gateway
        .WaitFor(applicationInsights)
        .WithReference(applicationInsights);
}

try
{
    builder.Build().Run();
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
    {
        Console.WriteLine(inner.Message);
        Console.WriteLine(inner.StackTrace);
    }
    throw;
}
