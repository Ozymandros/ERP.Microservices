using ErpApiGateway.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
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

var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{environment}.json", optional: true, reloadOnChange: true);

ApplyOcelotBaseUrlFromEnvironment(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedPrefix;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddOcelot(builder.Configuration).AddPolly();
builder.Services.AddMvcCore().AddApiExplorer();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

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

builder.Services.AddHealthChecks()
    .AddCheck("Gateway", () =>
    {
        return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            "Gateway is operational");
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Logging.AddDebug();
}

var app = builder.Build();

app.UseForwardedHeaders();

app.UseHealthChecks("/health");
app.UseHealthChecks("/health/live");
app.UseHealthChecks("/health/ready");

app.UseRouting();

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
        app.MapScalarApiReference("/scalar", (options, httpContext) =>
        {
            options.WithTitle("ERP Centralized Gateway API")
                   .WithTheme(ScalarTheme.Moon)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            var publicBaseUrl = GatewayUrlResolver.GetPublicBaseUrl(httpContext, configuration);

            var servers = new List<Scalar.AspNetCore.ScalarServer>();
            for (var i = 0; i < endpoints.Count; i++)
            {
                var (serviceDisplayName, upstreamPath) = endpoints[i];
                var servicePrefix = upstreamPath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                var serverUrl = $"{publicBaseUrl}/{servicePrefix}";
                servers.Add(new(serverUrl, serviceDisplayName));
                options.AddDocument(servicePrefix, serviceDisplayName, upstreamPath, isDefault: i == 0);
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

static void ApplyOcelotBaseUrlFromEnvironment(ConfigurationManager configuration)
{
    // Env Ocelot__GlobalConfiguration__BaseUrl -> configuration key Ocelot:GlobalConfiguration:BaseUrl
    var ocelotBaseUrl = configuration["Ocelot:GlobalConfiguration:BaseUrl"];
    if (string.IsNullOrWhiteSpace(ocelotBaseUrl))
    {
        return;
    }

    configuration["GlobalConfiguration:BaseUrl"] = ocelotBaseUrl.TrimEnd('/');
}
