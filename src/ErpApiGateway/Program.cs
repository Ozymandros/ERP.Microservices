using Microsoft.AspNetCore.OpenApi;
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
using System.Text.RegularExpressions;

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
            ValidIssuer = options.Authority,
            ValidateAudience = !environment.Equals("Development", StringComparison.OrdinalIgnoreCase),
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10)
        };
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata"); // Per a entorns de desenvolupament/Docker
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

        // Get the Routes section directly as children
        var routes = configuration.GetSection("Routes").GetChildren();

        if (routes is not null)
            foreach (var route in routes)
            {
                // Read the UpstreamPathTemplate property value of each route
                var upstreamPath = route.GetValue<string>("UpstreamPathTemplate");

                if (!string.IsNullOrEmpty(upstreamPath) && upstreamPath.Contains("openapi/v1.json"))
                {
                    // Extract the service name (the first segment after the slash)
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

// Surgical middleware to rewrite OpenAPI JSON URLs
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.Contains("openapi/v1.json") == true)
    {
        var originalBody = context.Response.Body;
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        await next();

        if (context.Response.StatusCode == 200)
        {
            newBody.Position = 0;
            var content = await new StreamReader(newBody).ReadToEndAsync();

            // 1. Detect the Gateway URL (ex: http://localhost:5000)
            var gatewayUrl = $"{context.Request.Scheme}://{context.Request.Host}";

            // 2. Extract the service prefix from the request URL itself
            // If the request is /auth/openapi/v1.json, the prefix is /auth
            var pathParts = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var servicePrefix = pathParts.Length > 0 ? $"/{pathParts[0]}" : "";
            var gatewayBaseUrl = $"{gatewayUrl}{servicePrefix}";

            // 3. Replace all occurrences of microservice internal URLs with Gateway URL
            // This handles:
            // - "servers" block URLs (e.g., "http://auth-service:8080" -> "http://localhost:5000/auth")
            // - Any other absolute URLs that might appear in the OpenAPI spec
            var modifiedContent = content;

            // Replace common microservice URL patterns
            modifiedContent = Regex.Replace(
                modifiedContent,
                @"http://[a-zA-Z0-9\-]+-service(:\d+)?",
                gatewayBaseUrl,
                RegexOptions.IgnoreCase);

            // Also replace the "url" field specifically (fallback for other formats)
            modifiedContent = Regex.Replace(
                modifiedContent,
                "\"url\"\\s*:\\s*\"[^\"]*\"",
                $"\"url\": \"{gatewayBaseUrl}\"");

            var buffer = Encoding.UTF8.GetBytes(modifiedContent);
            context.Response.Body = originalBody;
            context.Response.ContentLength = buffer.Length;
            await context.Response.Body.WriteAsync(buffer);
        }
        else
        {
            newBody.Position = 0;
            await newBody.CopyToAsync(originalBody);
        }
    }
    else
    {
        await next();
    }
});

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
