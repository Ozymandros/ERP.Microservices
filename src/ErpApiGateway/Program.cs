using Microsoft.AspNetCore.HttpOverrides; // <-- REQUIRED FOR PROXY HEADERS
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

// ========================================
// Configuration
// ========================================

var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{environment}.json", optional: true, reloadOnChange: true);

var ocelotRoutes = builder.Configuration.GetSection("Routes").GetChildren();
var microserviceNames = new HashSet<string>();

// ========================================
// Services
// ========================================

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
        options.Authority = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:6001";
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
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata");
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
// Middleware Configuration
// ========================================

// 1. DYNAMIC CLOUD FIX: Force app to read standard reverse-proxy headers (Azure, AWS, Codespaces, etc.)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Health check endpoints
app.UseHealthChecks("/health");
app.UseHealthChecks("/health/live");
app.UseHealthChecks("/health/ready");

app.UseRouting();

// Configure DocFX static files
string sitePath;
if (Directory.Exists("/_site")) sitePath = "/_site";
else if (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "_site"))) sitePath = Path.Combine(AppContext.BaseDirectory, "_site");
else
{
    var relativePath = Path.Combine(builder.Environment.ContentRootPath, "..", "_site");
    sitePath = Path.GetFullPath(relativePath);

    var contentRootFullPath = Path.GetFullPath(builder.Environment.ContentRootPath);
    if (!sitePath.StartsWith(contentRootFullPath, StringComparison.OrdinalIgnoreCase) &&
        !sitePath.StartsWith(Path.GetDirectoryName(contentRootFullPath) ?? "", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Resolved site path '{sitePath}' is outside expected directory structure.");
    }
}

if (Directory.Exists(sitePath))
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        if (path == "/docs" || path == "/docs/")
        {
            context.Response.Redirect("/docs/index.html", permanent: false);
            return;
        }
        await next();
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sitePath),
        RequestPath = "/docs"
    });
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("DocFX _site directory not found.");
}

if (app.Environment.IsDevelopment())
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    var routesConfig = configuration.GetSection("Routes").GetChildren();
    var endpoints = new List<(string ServiceDisplayName, string UpstreamPath)>();

    foreach (var route in routesConfig)
    {
        var upstreamPath = route.GetValue<string>("UpstreamPathTemplate");
        if (!string.IsNullOrEmpty(upstreamPath) && upstreamPath.EndsWith("openapi/v1.json"))
        {
            var parts = upstreamPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var servicePrefix = parts[0];
                var serviceDisplayName = $"{char.ToUpperInvariant(servicePrefix[0])}{servicePrefix.Substring(1)} Service";
                endpoints.Add((serviceDisplayName, upstreamPath));
            }
        }
    }

    if (endpoints.Count > 0)
    {
        // 2. DYNAMIC CLOUD FIX: Use the MapScalarApiReference lambda to read HttpContext runtime values
        app.MapScalarApiReference("/scalar", (options, httpContext) =>
        {
            options.WithTitle("ERP Centralized Gateway API")
                   .WithTheme(ScalarTheme.Moon)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            // This calculates the real external address (whether on localhost, Azure, AWS, or Codespaces)
            var dynamicBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var servers = new List<Scalar.AspNetCore.ScalarServer>();
            for (int i = 0; i < endpoints.Count; i++)
            {
                var (serviceDisplayName, upstreamPath) = endpoints[i];
                var servicePrefix = upstreamPath.Split('/')[1];

                // Build server endpoint cleanly from the dynamically calculated address
                var serverUrl = $"{dynamicBaseUrl}/{servicePrefix}";
                servers.Add(new(serverUrl, serviceDisplayName));

                bool isDefault = i == 0;
                options.AddDocument(servicePrefix, serviceDisplayName, upstreamPath, isDefault: isDefault);
            }

            options.Servers = servers;
        }).ShortCircuit();
    }
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();
app.Run();

record OcelotRoute
{
    public string UpstreamPathTemplate { get; set; } = string.Empty;
}