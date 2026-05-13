using Aspire.Hosting.Azure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Constants;

var isDeployment =
    args.Contains("--publisher") || // when azd generates manifests
    Environment.GetEnvironmentVariable("IS_DEPLOYMENT") == "true";

var builder = DistributedApplication.CreateBuilder(args).AddDapr();

// Dapr components: PubSub and State Store
// Note: Placement service is NOT needed - only required for Dapr Actors (we don't use actors)
// Note: Scheduler service is NOT needed - only required for scheduled jobs/workflows (we don't use)
// The connection errors for Placement (6050) and Scheduler (6060) are harmless warnings.
var stateStore = builder.AddDaprStateStore("statestore");
var pubSub = builder.AddDaprPubSub(MessagingConstants.PubSubName);

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
IResourceBuilder<SqlServerServerResource>? sqlServer = null;
IResourceBuilder<AzureSqlServerResource>? sqlAzure = null;

// Add SQL Server as a container
var password = builder.AddParameter("password", secret: true, value: "Your_strong_(!)Password123");
if (isDeployment)
{
    sqlAzure = builder.AddAzureSqlServer("myapp-sqlserver");
    projectBuilder = builder.CreateProjectBuilder(sqlAzure: sqlAzure);
}
else
{
    sqlServer = builder.AddSqlServer("myapp-sqlserver", password, 1455)
        .WithImage("mssql/server", "2025-latest")
        .WithImageRegistry("mcr.microsoft.com")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("sqlserver-data");
    projectBuilder = builder.CreateProjectBuilder(sqlServer: sqlServer);
}

var origin = builder.Configuration["Parameters:FrontendOrigin"];

// Get JWT configuration from appsettings.json or use defaults
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "a_very_long_and_super_ultra_secret_key_01234566789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MyApp.Auth";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MyApp.All";

// Add projects - ports auto-increment
var authService = projectBuilder.AddWebProject<Projects.MyApp_Auth_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: AuthDB, auth-service, ports 6001, 3501, 46001, 9091

var billingService = projectBuilder.AddWebProject<Projects.MyApp_Billing_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: BillingDB, billing-service, ports 6002, 3502, 45002, 9092

var crmService = projectBuilder.AddWebProject<Projects.MyApp_Crm_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: CrmDB, crm-service, ports 6003, 3503, 45003, 9093

var inventoryService = projectBuilder.AddWebProject<Projects.MyApp_Inventory_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: InventoryDB, inventory-service, ports 6004, 3504, 45004, 9094

var ordersService = projectBuilder.AddWebProject<Projects.MyApp_Orders_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: OrderDB, orders-service, ports 6005, 3505, 45005, 9095

var purchasingService = projectBuilder.AddWebProject<Projects.MyApp_Purchasing_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: PurchasingDB, purchasing-service, ports 6006, 3506, 45006, 9096

var salesService = projectBuilder.AddWebProject<Projects.MyApp_Sales_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore);
// Creates: SalesDB, sales-service, ports 6007, 3507, 45007, 9097

var agenticService = projectBuilder.AddWebProject<Projects.MyApp_Agentic_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore, hasDatabase: true);
// Creates: agentic-service (no DB), ports 6008, 3508, 45008, 9098

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
    .WaitFor(crmService)
    .WaitFor(agenticService)
    .WithHttpEndpoint(port: 5000, name: "gateway-http")   // Explicitly listen on 5000 for Dapr
    .WithHttpsEndpoint(port: 7231, name: "gateway-https") // Explicitly listen on 7231 for Browser/Scalar
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("OCELOT_ENVIRONMENT", "Development");

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
