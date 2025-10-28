
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Aquesta línia registra el DaprClient (Singleton) al contenidor d'Injecció de Dependències (DI)
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("PurchasingDb") 
    ?? "Server=localhost;Database=PurchasingDb;Trusted_Connection=True;";
builder.Services.AddDbContext<PurchasingDbContext>(options =>
    options.UseSqlServer(connectionString));

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
builder.Services.AddCustomHealthChecks(connectionString);

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

// Afegeix un bloc per a l'aplicaci� autom�tica de les migracions
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
    // ATENCI�: Esborra la base de dades i torna-la a crear si est� buida (�til per a desenvolupament amb contenidors)
    // dbContext.Database.EnsureDeleted(); 

    // Aquest �s el m�tode clau: aplica les migracions pendents.
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            components = report.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
