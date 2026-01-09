using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.OpenApi;
using Scalar.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// This line registers the DaprClient (Singleton) in the Dependency Injection (DI) container
builder.Services.AddDaprClient();

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "MyApp.Purchasing.API";

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

// Add services to the container
// Configure JSON options FIRST - before AddOpenApi() so JsonSchemaExporter uses them
// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.SerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

// Configure JSON options for Controllers as well
// builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
// {
//     options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.JsonSerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
    options.AddDocumentTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaDocumentTransformer>();

    // Force the type at schema level to prevent the internal serializer from breaking
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
            schema.Format = "date-time";
            schema.Default = null; // Prevents the engine from trying to serialize a default(DateTime)
            schema.Example = null;
        }
        return Task.CompletedTask;
    });

    // Also add the shared transformer as a fallback
    options.AddSchemaTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaTransformer>();
});

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("PurchasingDb")
    ?? "Server=localhost;Database=PurchasingDb;Trusted_Connection=True;";
builder.Services.AddDbContext<PurchasingDbContext>(options =>
    options.UseSqlServer(connectionString, options => options.EnableRetryOnFailure()));

builder.Services.AddHttpContextAccessor();

// Repository registration
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IPurchaseOrderLineRepository, PurchaseOrderLineRepository>();

// Service registration
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

builder.AddRedisDistributedCache("cache");
builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "api" });

// AutoMapper registration
builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Purchasing.Application.Mappings.PurchasingMappingProfile).Assembly
);

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

// Add a block for automatic migration application
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
    // WARNING: Deletes the database and recreates it if empty (useful for development with containers)
    // dbContext.Database.EnsureDeleted(); 

    // This is the key method: applies pending migrations.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
// Map OpenAPI endpoint (available for Gateway access)
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    // Use Scalar instead of SwaggerUI - lighter and more stable
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.UseCustomHealthChecks();

// Dapr pub/sub subscriptions
app.MapSubscribeHandler();

app.Run();
