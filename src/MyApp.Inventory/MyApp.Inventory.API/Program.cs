using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.OpenApi;
using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Aquesta línia registra el DaprClient (Singleton) al contenidor d'Injecció de Dependències (DI)
builder.Services.AddDaprClient();

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "MyApp.Inventory.API";

// Configure OpenTelemetry pipeline.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
});
// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Get connection string
var inventoryDbConnectionString = builder.Configuration.GetConnectionString("inventorydb");

// Health Checks
builder.Services.AddCustomHealthChecks(inventoryDbConnectionString ?? throw new InvalidOperationException("Connection string 'inventorydb' not found."));

// Infrastructure & Application DI
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(inventoryDbConnectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IInventoryTransactionRepository, MyApp.Inventory.Infrastructure.Data.Repositories.InventoryTransactionRepository>();
builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IProductRepository, MyApp.Inventory.Infrastructure.Data.Repositories.ProductRepository>();
builder.Services.AddScoped<MyApp.Inventory.Domain.Repositories.IWarehouseRepository, MyApp.Inventory.Infrastructure.Data.Repositories.WarehouseRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Inventory.Application.Mappings.InventoryMappingProfile).Assembly
);
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IInventoryTransactionService, MyApp.Inventory.Application.Services.InventoryTransactionService>();
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IProductService, MyApp.Inventory.Application.Services.ProductService>();
builder.Services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IWarehouseService, MyApp.Inventory.Application.Services.WarehouseService>();

builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

builder.AddRedisDistributedCache("cache");
builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();

var origins = builder.Configuration["FRONTEND_ORIGIN"]?.Split(';') ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    // ATENCI�: Esborra la base de dades i torna-la a crear si est� buida (�til per a desenvolupament amb contenidors)
    // dbContext.Database.EnsureDeleted(); 

    // Aquest �s el m�tode clau: aplica les migracions pendents.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Inventory API v1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add health checks endpoint
app.UseCustomHealthChecks();

app.Run();
