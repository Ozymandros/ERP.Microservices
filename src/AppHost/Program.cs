using AppHost;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Constants;

var isDeployment =
    args.Contains("--publisher") || // when azd generates manifests
    Environment.GetEnvironmentVariable("IS_DEPLOYMENT") == "true";

var builder = DistributedApplication.CreateBuilder(args).AddDapr();

var jwtSecretKey = builder.AddParameter("jwt-secret", secret: true, value: "DevOnlyLocalJwtSecretKey32CharsMinimum!");

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

// Dapr pub/sub + state: explicit Redis component types (not building-block in-memory fallbacks).
// redisHost must be host:port only — never a URL. enableTLS must match Aspire Redis tcp endpoint.
var redisTcp = redis.GetEndpoint("tcp");
var redisHost = redisTcp.Property(EndpointProperty.HostAndPort);
var redisTls = redisTcp.Property(EndpointProperty.TlsEnabled);

var stateStore = builder.AddDaprComponent("statestore", "state.redis")
    .WithMetadata("redisHost", redisHost)
    .WithMetadata("redisPassword", redis.Resource.PasswordParameter!)
    .WithMetadata("enableTLS", redisTls)
    .WaitFor(redis);

var pubSub = builder.AddDaprComponent(MessagingConstants.PubSubName, "pubsub.redis")
    .WithMetadata("redisHost", redisHost)
    .WithMetadata("redisPassword", redis.Resource.PasswordParameter!)
    .WithMetadata("enableTLS", redisTls)
    .WaitFor(redis);

// Create builder with automatic port management
AspireProjectBuilder projectBuilder;
IResourceBuilder<SqlServerServerResource>? sqlServer = null;
IResourceBuilder<AzureSqlServerResource>? sqlAzure = null;

// Add SQL Server as a container
var password = builder.AddParameter("password", secret: true, value: "Your_strong_(!)Password123");
if (isDeployment)
{
    sqlAzure = builder.AddAzureSqlServer("myapp-sqlserver");
    projectBuilder = builder.CreateProjectBuilder(sqlAzure: sqlAzure, jwtSecretKey: jwtSecretKey);
}
else
{
    sqlServer = builder.AddSqlServer("myapp-sqlserver", password, 1455)
        .WithImage("mssql/server", "2025-latest")
        .WithImageRegistry("mcr.microsoft.com")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("sqlserver-data");
    projectBuilder = builder.CreateProjectBuilder(sqlServer: sqlServer, jwtSecretKey: jwtSecretKey);
}

var origin = builder.Configuration["Parameters:AllowedOrigins"]
    ?? builder.Configuration["Parameters:FrontendOrigin"];

// JWT signing key is injected via Aspire secret parameter (Jwt__SecretKey env var)
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

var auditService = projectBuilder.AddWebProject<Projects.MyApp_Audit_API>(redis, origin, isDeployment, applicationInsights, pubSub, stateStore, hasDatabase: true);
// Creates: audit-service (no DB), ports 6009, 3509, 45009, 9099

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
    .WaitFor(auditService)
    .WithHttpEndpoint(port: 5000, name: "gateway-http")   // Explicitly listen on 5000 for Dapr
    .WithHttpsEndpoint(port: 7231, name: "gateway-https") // Explicitly listen on 7231 for Browser/Scalar
    .WithEnvironment("Jwt__SecretKey", jwtSecretKey)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("OCELOT_ENVIRONMENT", "Development");

var codespaceGatewayBaseUrl = GetCodespacesForwardedGatewayUrl(5000);
if (!string.IsNullOrWhiteSpace(codespaceGatewayBaseUrl))
{
    gateway = gateway.WithEnvironment("Ocelot__GlobalConfiguration__BaseUrl", codespaceGatewayBaseUrl);
}

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

static string? GetCodespacesForwardedGatewayUrl(int port)
{
    var codespaceName = Environment.GetEnvironmentVariable("CODESPACE_NAME");
    var forwardingDomain = Environment.GetEnvironmentVariable("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN");
    if (string.IsNullOrWhiteSpace(codespaceName) || string.IsNullOrWhiteSpace(forwardingDomain))
    {
        return null;
    }

    return $"https://{codespaceName}-{port}.{forwardingDomain}";
}
