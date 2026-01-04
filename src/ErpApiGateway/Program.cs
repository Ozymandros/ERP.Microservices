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

// Configure OpenTelemetry pipeline.nfdnhfd
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

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
});

// ========================================
// Configuration
// ========================================

// Configuración de Ocelot with environment-specific configuration
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{environment}.json", optional: true, reloadOnChange: true);

// ========================================
// Services
// ========================================

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration).AddPolly();

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = !environment.Equals("Development", StringComparison.OrdinalIgnoreCase),
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10)
        };
        // Si es Desarrollo, permitimos HTTP. Si no (Prod/Staging), HTTPS es obligatorio.
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }
        else
        {
            options.RequireHttpsMetadata = true;
        }
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        // Obtenim la secció Routes directament com a fills
        var routes = configuration.GetSection("Routes").GetChildren();

        if (routes is not null)
            foreach (var route in routes)
            {
                // Llegim el valor de la propietat UpstreamPathTemplate de cada ruta
                var upstreamPath = route.GetValue<string>("UpstreamPathTemplate");

                if (!string.IsNullOrEmpty(upstreamPath) && upstreamPath.Contains("openapi/v1.json"))
                {
                    // Extraiem el nom del servei (el primer segment després de la barra)
                    var parts = upstreamPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var serviceName = parts.Length > 0 ? parts[0].ToUpper() : "UNKNOWN";

                    options.SwaggerEndpoint(upstreamPath, $"{serviceName} API");
                }
            }
    });
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
