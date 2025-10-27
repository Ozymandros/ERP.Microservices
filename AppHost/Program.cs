using Microsoft.Extensions.DependencyInjection;

var isDeployment =
    args.Contains("--publisher") || // quan azd genera manifests
    Environment.GetEnvironmentVariable("IS_DEPLOYMENT") == "true";

var builder = DistributedApplication.CreateBuilder(args);

var analyticsWorkspace = isDeployment ? builder
    .AddAzureLogAnalyticsWorkspace("MyApp-LogAnalyticsWorkspace") : null;
var applicationInsights = isDeployment ? builder
    .AddAzureApplicationInsights("MyApp-ApplicationInsights")
    .WithLogAnalyticsWorkspace(analyticsWorkspace) : null;

builder.Services.AddHealthChecks();
//var store = builder.AddDaprStateStore("cache", new());
// Afegeix el contenidor de Redis

// 1. Definim el host de Redis. Li donem un nom sense conflictes.
// No importa el nom aquí, ja que el Component el definirà.
var redis = builder.AddRedis("cache")
    //.WaitFor(store)
    //.WithDaprSidecar()
    .WithRedisCommander()
    .WithRedisInsight()
    .WithDataVolume("redis-cache");

// Create builder with automatic port management
AspireProjectBuilder projectBuilder;

// Afegir SQL Server com a contenidor
var password = builder.AddParameter("password", secret: true);
if (isDeployment)
{
    var sqlServer = builder.AddAzureSqlServer("myapp-sqlserver");
    projectBuilder = builder.CreateProjectBuilder();
}
else
{
    var sqlServer = builder.AddSqlServer("myapp-sqlserver", password, 1455)
        .WithLifetime(ContainerLifetime.Persistent) // reinicia periòdicament per a entorns de desenvolupament
        .WithDataVolume("sqlserver-data"); // volum amb nom → es manté entre arrencades
    projectBuilder = builder.CreateProjectBuilder(sqlServer);
}

var origin = builder.Configuration["Parameters:FrontendOrigin"];

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

if (isDeployment)
{
    // Azure Deployment: ErpApiGateway with Ocelot
    var apiGateway = builder.AddProject<Projects.ErpApiGateway>("api-gateway")
        .WaitFor(authService)
        .WaitFor(billingService)
        .WaitFor(inventoryService)
        .WaitFor(ordersService)
        .WaitFor(purchasingService)
        .WaitFor(salesService)
        .WithExternalHttpEndpoints()
        .WithEnvironment("OCELOT_ENVIRONMENT", "Production")
        .PublishAsDockerFile();

    if (applicationInsights is not null)
    {
        apiGateway = apiGateway
            .WaitFor(applicationInsights)
            .WithReference(applicationInsights);
    }
}
else
{
    // Local Development: Reverse Proxy (YARP)
    var gateway = builder.AddYarp("gateway")
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
                         });
}

//TODO (o no):
//var webClient = builder.AddNpmApp("client", "../../erp-frontend", "dev")
//    .WithEnvironment("VITE_API", gateway.GetEndpoint("http"))
//    .WithExternalHttpEndpoints();

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
