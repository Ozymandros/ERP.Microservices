using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using MyApp.Shared.Infrastructure.OpenApi;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "ErpApiGateway";

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

// Configure JSON options for Controllers (for any JSON response from the Gateway)
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

// ========================================
// Configuration
// ========================================

// Configure Ocelot with environment-specific configuration
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{environment}.json", optional: true, reloadOnChange: true);

// According to Microsoft's official guidelines for .NET 10:
// The Gateway does NOT generate its own OpenAPI document (it has no controllers).
// It only configures "placeholder" documents for each microservice it consumes remotely.
// These documents do not look for local controllers, they only serve as identifiers.
// We read microservices dynamically from Ocelot's configuration.
var ocelotRoutes = builder.Configuration.GetSection("Routes").GetChildren();
var microserviceNames = new HashSet<string>();

// Note: Scalar does not need configuration in the builder when using MapScalarApiReference()
// Configuration is done directly in MapScalarApiReference() with the options

// ========================================
// Services
// ========================================

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration).AddPolly();

builder.Services.AddMvcCore().AddApiExplorer();

// Add Authentication - JWT Bearer
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JwtSecretKey configuration is required");
var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5001";
        options.Audience = builder.Configuration["Jwt:Audience"] ?? "erp-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = !environment.Equals("Development", StringComparison.OrdinalIgnoreCase),
            ValidIssuer = options.Authority,
            ValidateAudience = !environment.Equals("Development", StringComparison.OrdinalIgnoreCase),
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // Allows 30 seconds margin for Docker/container synchronization
        };
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata"); // For development/Docker environments
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Message}", context.Exception?.Message);
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new
                {
                    error = "Authentication failed",
                    message = context.Exception?.Message ?? "Invalid token"
                });
            }
        };
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// Add CORS
var origins = builder.Configuration["FRONTEND_ORIGIN"]?.Split(';') ?? new[] { "http://localhost:3000" };
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

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("Gateway", () =>
    {
        return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            "Gateway is operational");
    });

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Logging.AddDebug();
}

// ========================================
// Build App
// ========================================

var app = builder.Build();

// ========================================
// Middleware
// ========================================

// Health check endpoints (no auth required)
app.UseHealthChecks("/health");
app.UseHealthChecks("/health/live");
app.UseHealthChecks("/health/ready");

// Configure DocFX static files: Serve documentation at /docs
// The _site directory contains the generated DocFX documentation
var docfxPath = Path.Combine(builder.Environment.ContentRootPath, "..", "_site");
if (Directory.Exists(docfxPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(docfxPath),
        RequestPath = "/docs"
    });

    // Redirect /docs and /docs/ to /docs/index.html
    app.MapGet("/docs", () => Results.Redirect("/docs/index.html")).ShortCircuit();
    app.MapGet("/docs/", () => Results.Redirect("/docs/index.html")).ShortCircuit();
}

// According to Microsoft's official guidelines for .NET 10:
// The Gateway does NOT generate its own OpenAPI document (it has no controllers).
// Therefore, we do NOT call MapOpenApi() here.

if (app.Environment.IsDevelopment())
{
    // Centralized Gateway: Single Scalar interface with multiple dynamic endpoints
    // We read OpenAPI routes from Ocelot to create endpoints automatically
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    var routesConfig = configuration.GetSection("Routes").GetChildren();
    var endpoints = new List<(string ServiceDisplayName, string UpstreamPath)>();

    // Collect all OpenAPI endpoints dynamically
    foreach (var route in routesConfig)
    {
        var upstreamPath = route.GetValue<string>("UpstreamPathTemplate");

        // Look for routes that point to OpenAPI documents (e.g., /auth/openapi/v1.json)
        if (!string.IsNullOrEmpty(upstreamPath) && upstreamPath.EndsWith("openapi/v1.json"))
        {
            var parts = upstreamPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                // Extract the service name from the prefix (e.g., "auth" → "Auth Service")
                var servicePrefix = parts[0];
                var serviceDisplayName = $"{char.ToUpperInvariant(servicePrefix[0])}{servicePrefix.Substring(1)} Service";

                endpoints.Add((serviceDisplayName, upstreamPath));
            }
        }
    }

    // Create a SINGLE instance of Scalar with ALL dynamic endpoints
    // Scalar will show a dropdown to select between different services
    if (endpoints.Count > 0)
    {
        app.MapScalarApiReference("/scalar", options =>
        {
            options.WithTitle("ERP Centralized Gateway API")
                   .WithTheme(ScalarTheme.Moon)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            // Add ALL endpoints dynamically
            // Each endpoint can be selected via a dropdown within Scalar
            for (int i = 0; i < endpoints.Count; i++)
            {
                var (serviceDisplayName, upstreamPath) = endpoints[i];
                var serviceId = upstreamPath.Split('/')[1]; // e.g., "auth" from "/auth/openapi/v1.json"
                bool isDefault = i == 0; // The first service is the default

                options.AddDocument(serviceId, serviceDisplayName, upstreamPath, isDefault: isDefault);
            }
        }).ShortCircuit(); // CRITICAL: Prevents Ocelot from processing the /scalar route
    }
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AllowFrontend");

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Ocelot middleware - must be last
await app.UseOcelot();

app.Run();

record OcelotRoute
{
    public string UpstreamPathTemplate { get; set; } = string.Empty;
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

