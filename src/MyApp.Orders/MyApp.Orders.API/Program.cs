using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Infrastructure.Data;
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

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "MyApp.Orders.API";

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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
    options.AddDocumentTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaDocumentTransformer>();

    // Forçem el tipus a nivell d'esquema per evitar que el serialitzador intern pete
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
            schema.Format = "date-time";
            schema.Default = null; // Evita que el motor intenti serialitzar un default(DateTime)
            schema.Example = null;
        }
        return Task.CompletedTask;
    });

    // També afegim el transformer compartit com a fallback
    options.AddSchemaTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaTransformer>();
});

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Infrastructure & Application DI
var ordersDbConnectionString = builder.Configuration.GetConnectionString("ordersdb")
    ?? throw new InvalidOperationException("Connection string 'ordersdb' not found.");
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(ordersDbConnectionString, options => options.EnableRetryOnFailure()));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MyApp.Orders.Domain.IOrderRepository, MyApp.Orders.Infrastructure.Repositories.OrderRepository>();
builder.Services.AddScoped<MyApp.Orders.Domain.IOrderLineRepository, MyApp.Orders.Infrastructure.Repositories.OrderLineRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Orders.Application.Mapping.OrderProfile).Assembly
);
builder.Services.AddScoped<MyApp.Orders.Application.Contracts.IOrderService, MyApp.Orders.Application.Services.OrderService>();

builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

builder.AddRedisDistributedCache("cache");
builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();

// Add health checks
builder.Services.AddCustomHealthChecks(ordersDbConnectionString);

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
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    // WARNING: Deletes the database and recreates it if empty (useful for development with containers)
    // dbContext.Database.EnsureDeleted(); 

    // This is the key method: applies pending migrations.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
