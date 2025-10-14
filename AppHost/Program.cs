using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

// Afegir SQL Server com a contenidor
var password = builder.AddParameter("password", secret: true);
var sqlServer = builder.AddSqlServer("sqlserver", password);

// Afegir cada base de dades com a recurs independent
var inventoryDb = sqlServer.AddDatabase("InventoryDB");
var ordersDb = sqlServer.AddDatabase("OrdersDB");
var usersDb = sqlServer.AddDatabase("UsersDB");
var salesDb = sqlServer.AddDatabase("SalesDB");
var billingDb = sqlServer.AddDatabase("BillingDB");
var notificationsDb = sqlServer.AddDatabase("NotificationsDB");
var purchasingDb = sqlServer.AddDatabase("PurchasingDB");

// Afegir el teu microservici
builder.AddRedis("redis").WithDaprSidecar(new DaprSidecarOptions
{
    AppId = "redis",
    AppPort = 6380, // Port alternatiu
    DaprHttpPort = 3600,
    DaprGrpcPort = 51000,
    MetricsPort = 9190
});

// Registra los microservicios
var inventoryService = builder.AddProject<Projects.MyApp_Inventory_API>("inventoryservice");
inventoryService = inventoryService
    .WithReference(inventoryDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "inventoryservice",
        AppPort = 5001,
        DaprGrpcPort = 50001,
        DaprHttpPort = 3501,
        MetricsPort = 9091
    });

var ordersService = builder.AddProject<Projects.MyApp_Orders_API>("ordersservice");
ordersService = ordersService
    .WithReference(ordersDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "ordersservice",
        AppPort = 5002,
        DaprHttpPort = 3502,
        DaprGrpcPort = 50002,
        MetricsPort = 9092
    });

var salesService = builder.AddProject<Projects.MyApp_Sales_API>("salesservice");
salesService = salesService
    .WithReference(salesDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "salesservice",
        AppPort = 5003,
        DaprHttpPort = 3503,
        DaprGrpcPort = 50003,
        MetricsPort = 9093
    });

var billingService = builder.AddProject<Projects.MyApp_Billing_API>("billingservice");
billingService = billingService
    .WithReference(billingDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "billingservice",
        AppPort = 5004,
        DaprHttpPort = 3504,
        DaprGrpcPort = 50004,
        MetricsPort = 9094
    });

var notificationService = builder.AddProject<Projects.MyApp_Notification_API>("notificationservice");
notificationService = notificationService
    .WithReference(notificationsDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "notificationservice",
        AppPort = 5005,
        DaprHttpPort = 3505,
        DaprGrpcPort = 50005,
        MetricsPort = 9095
    });

var purchasingService = builder.AddProject<Projects.MyApp_Purchasing_API>("purchasingservice");
purchasingService = purchasingService
    .WithReference(purchasingDb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "purchasingservice",
        AppPort = 5006,
        DaprHttpPort = 3506,
        DaprGrpcPort = 50006,
        MetricsPort = 9096
    });

// Registra el API Gateway de Ocelot
var apiGateway = builder.AddProject<Projects.ErpApiGateway>("apigateway")
// Wire remaining services into the gateway
    .WithReference(inventoryService)
    .WithReference(ordersService)
    .WithReference(salesService)
    .WithReference(salesService)
    .WithReference(billingService)
    .WithReference(notificationService)
    .WithReference(purchasingService);

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

builder.Build().Run();
