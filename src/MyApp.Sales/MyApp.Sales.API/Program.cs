using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Infrastructure.Data;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Aquesta línia registra el DaprClient (Singleton) al contenidor d'Injecció de Dependències (DI)
builder.Services.AddDaprClient();

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "MyApp.Sales.API";

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
var salesDbConnectionString = builder.Configuration.GetConnectionString("salesdb");

// Health Checks
builder.Services.AddCustomHealthChecks(salesDbConnectionString ?? throw new InvalidOperationException("Connection string 'salesdb' not found."));

// Infrastructure & Application DI
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(salesDbConnectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MyApp.Sales.Domain.ISalesOrderLineRepository, MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderLineRepository>();
builder.Services.AddScoped<MyApp.Sales.Domain.ISalesOrderRepository, MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderRepository>();
builder.Services.AddScoped<MyApp.Sales.Domain.ICustomerRepository, MyApp.Sales.Infrastructure.Data.Repositories.CustomerRepository>();

builder.Services.AddAutoMapper(
    cfg => { /* optional configuration */ },
    typeof(MyApp.Sales.Application.Mapping.SalesOrderMappingProfile).Assembly
);
builder.Services.AddScoped<MyApp.Sales.Application.Contracts.Services.ISalesOrderService, MyApp.Sales.Application.Services.SalesOrderService>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
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
        options.SwaggerEndpoint("/openapi/v1.json", "Sales API v1");
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
