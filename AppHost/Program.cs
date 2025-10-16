using Aspire.Hosting.Yarp.Transforms;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

// Afegir SQL Server com a contenidor
var password = builder.AddParameter("password", secret: true);
var sqlServer = builder.AddSqlServer("sqlserver", password);

var usersDb = sqlServer.AddDatabase("UsersDB");
//var notificationsDb = sqlServer.AddDatabase("NotificationsDB");

// Afegir el teu microserviciAppId 
builder.AddRedis("redis").WithDaprSidecar(new DaprSidecarOptions
{
    AppId = "redis",
    //AppPort = 6380, // Port alternatiu
    DaprHttpPort = 3600,
    DaprGrpcPort = 45000,
    MetricsPort = 9190
});

var inventoryDb = sqlServer.AddDatabase("InventoryDB");
var inventoryervice = builder.AddProject<Projects.MyApp_Inventory_API>("inventoryervice");
inventoryervice = inventoryervice
    .WithHttpEndpoint(5001)
    .WithExternalHttpEndpoints()
    .WaitFor(inventoryDb)
    .WithReference(inventoryDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "inventoryervice",
        AppPort = 5001,
        DaprGrpcPort = 45001,
        DaprHttpPort = 3501,
        MetricsPort = 9091
    });

var ordersDb = sqlServer.AddDatabase("OrdersDB");
var ordersService = builder.AddProject<Projects.MyApp_Orders_API>("orders-service");
ordersService = ordersService
    .WithHttpEndpoint(5002)
    .WithExternalHttpEndpoints()
    .WaitFor(ordersDb)
    .WithReference(ordersDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "orders-service",
        //AppPort = 5002,
        DaprHttpPort = 3502,
        DaprGrpcPort = 45002,
        MetricsPort = 9092
    });

var salesDb = sqlServer.AddDatabase("SalesDB");
var salesService = builder.AddProject<Projects.MyApp_Sales_API>("sales-service");
salesService = salesService
    .WithHttpEndpoint(5003)
    .WithExternalHttpEndpoints()
    .WaitFor(salesDb)
    .WithReference(salesDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "sales-service",
        AppPort = 5003,
        DaprHttpPort = 3503,
        DaprGrpcPort = 45003,
        MetricsPort = 9093
    });

var billingDb = sqlServer.AddDatabase("BillingDB");
var billingService = builder.AddProject<Projects.MyApp_Billing_API>("billing-service");
billingService = billingService
    .WithHttpEndpoint(5004)
    .WithExternalHttpEndpoints()
    .WaitFor(billingDb)
    .WithReference(billingDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "billing-service",
        AppPort = 5004,
        DaprHttpPort = 3504,
        DaprGrpcPort = 45004,
        MetricsPort = 9094
    });

//var notificationService = builder.AddProject<Projects.MyApp_Notification_API>("notification-service");
//notificationService = notificationService
//    .WithHttpEndpoint()
//    .WithExternalHttpEndpoints()
//    .WithReference(notificationsDb)
//    .WithDaprSidecar(new DaprSidecarOptions
//    {
//        AppId = "notification-service",
//        AppPort = 5005,
//        DaprHttpPort = 3505,
//        DaprGrpcPort = 45005,
//        MetricsPort = 9095
//    });

var purchasingDb = sqlServer.AddDatabase("PurchasingDB");
var purchasingService = builder.AddProject<Projects.MyApp_Purchasing_API>("purchasing-service");
purchasingService = purchasingService
    .WithHttpEndpoint(5006)
    .WithExternalHttpEndpoints()
    .WaitFor(purchasingDb)
    .WithReference(purchasingDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "purchasing-service",
        AppPort = 5006,
        DaprHttpPort = 3506,
        DaprGrpcPort = 45006,
        MetricsPort = 9096
    });
/*
// Registra el API Gateway de Ocelot
var apiGateway = builder.AddProject<Projects.ErpApiGateway>("apigateway")
    .WithHttpEndpoint(5000)
    .WithExternalHttpEndpoints()
// Wire remaining services into the gateway
    .WithReference(inventoryervice)
    .WithReference(ordersService)
    .WithReference(salesService)
    .WithReference(salesService)
    .WithReference(billingService)
    //.WithReference(notificationService)
    .WithReference(purchasingService);
//.WithEndpoint(name: "http", port: 5000, isProxied: true); ;

// attach sidecar to the API gateway itself (listening port 5000)
apiGateway = apiGateway
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "apigateway",
        AppPort = 5000,
        DaprHttpPort = 3500,
        DaprGrpcPort = 50000,
        MetricsPort = 9090
    });
*/

var authDb = sqlServer.AddDatabase("AuthDb");
var authService = builder.AddProject<Projects.MyApp_Auth_API>("auth-service")
    .WithHttpEndpoint(5007)
    .WithExternalHttpEndpoints()
    .WaitFor(authDb)
    .WithReference(authDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "purchasing-service",
        AppPort = 5007,
        DaprHttpPort = 3507,
        DaprGrpcPort = 45007,
        MetricsPort = 9097
    });

// Configuració del Reverse Proxy (YARP)
var gateway = builder.AddYarp("gateway")
                     //.WithHttpEndpoint(5000)
                     .WithConfiguration(yarp =>
                     {
                         // Configure routes programmatically
                         yarp.AddRoute("/inventory/{**catch-all}", inventoryervice)
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
