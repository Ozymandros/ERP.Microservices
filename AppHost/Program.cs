using Aspire.Hosting.Yarp.Transforms;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

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
var sqlServer = builder.AddSqlServer("sqlserver", password).WithDataVolume("sqlserver-data"); // volum amb nom ? es manté entre arrencades

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
    .WaitFor(authService)
    .WaitFor(billingService)
    .WaitFor(inventoryService)
    .WaitFor(ordersService)
    .WaitFor(purchasingService)
    .WaitFor(salesService)
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
