using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for common microservice configuration
/// </summary>
public static class MicroserviceExtensions
{
    /// <summary>
    /// Adds all common microservice services to the service collection
    /// </summary>
    public static WebApplicationBuilder AddCommonMicroserviceServices(
        this WebApplicationBuilder builder,
        MicroserviceConfigurationOptions? options = null)
    {
        options ??= new MicroserviceConfigurationOptions();

        // Set service name for telemetry
        var serviceName = options.ServiceName 
            ?? builder.Environment.ApplicationName 
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name 
            ?? "UnknownService";

        // 1. DAPR Client
        if (options.EnableDapr)
        {
            builder.Services.AddDaprClient();
        }

        // 2. OpenTelemetry
        if (options.EnableOpenTelemetry)
        {
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
        }

        // 3. Controllers and API endpoints
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // 4. OpenAPI/Swagger with custom transformers
        builder.Services.AddOpenApi(apiOptions =>
        {
            apiOptions.AddDocumentTransformer<JwtSecuritySchemeDocumentTransformer>();
            apiOptions.AddDocumentTransformer<DateTimeSchemaDocumentTransformer>();

            // Force DateTime schema to string format
            apiOptions.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
                {
                    schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
                    schema.Format = "date-time";
                    schema.Default = null;
                    schema.Example = null;
                }
                return Task.CompletedTask;
            });

            apiOptions.AddSchemaTransformer<DateTimeSchemaTransformer>();
        });

        // 5. JWT Authentication
        if (options.EnableAuthentication)
        {
            builder.Services.AddJwtAuthentication(builder.Configuration);
        }

        // 6. Database Context
        if (options.DbContextType != null && !string.IsNullOrEmpty(options.ConnectionStringKey))
        {
            var connectionString = builder.Configuration.GetConnectionString(options.ConnectionStringKey)
                ?? throw new InvalidOperationException($"Connection string '{options.ConnectionStringKey}' not found.");

            var addDbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods()
                .First(m => m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == 3);

            var genericMethod = addDbContextMethod.MakeGenericMethod(options.DbContextType);
            genericMethod.Invoke(null, new object[]
            {
                builder.Services,
                (Action<DbContextOptionsBuilder>)(opts =>
                    opts.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure())),
                ServiceLifetime.Scoped,
                ServiceLifetime.Scoped
            });

            // Health checks (include database check)
            if (options.EnableHealthChecks)
            {
                builder.Services.AddCustomHealthChecks(connectionString);
            }
        }
        else if (options.EnableHealthChecks)
        {
            // Health checks without database
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "api" });
        }

        // 7. HTTP Context Accessor
        builder.Services.AddHttpContextAccessor();

        // 8. Permission Checker
        if (options.EnableDapr)
        {
            builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();
        }

        // 9. Redis Distributed Cache (caller must configure - Aspire handles this in Program.cs)
        if (options.EnableRedisCache)
        {
            // Note: Redis configuration expected to be handled by Aspire's AddRedisDistributedCache
            // This is typically called in the service's Program.cs before calling this method
            builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();
        }

        // 10. AutoMapper (if assembly provided)
        // Note: AutoMapper setup is expected to be handled by the service
        // This typically involves calling services.AddAutoMapper() with the profile assembly

        // 11. CORS
        var origins = builder.Configuration["FRONTEND_ORIGIN"]?.Split(';') ?? new[] { "http://localhost:3000" };
        builder.Services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // 12. Service-specific dependencies
        options.ConfigureServiceDependencies?.Invoke(builder.Services);

        return builder;
    }

    /// <summary>
    /// Applies common microservice middleware pipeline
    /// </summary>
    public static WebApplication UseCommonMicroservicePipeline(
        this WebApplication app,
        MicroserviceConfigurationOptions? options = null)
    {
        options ??= new MicroserviceConfigurationOptions();

        // Apply database migrations if DbContext is configured
        if (options.DbContextType != null)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService(options.DbContextType) as DbContext;
                if (dbContext != null)
                {
                    dbContext.Database.Migrate();
                }
            }
        }

        // Map OpenAPI endpoint
        app.MapOpenApi();

        // Scalar API documentation (development only)
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
        }

        // HTTPS redirection
        app.UseHttpsRedirection();

        // Routing
        app.UseRouting();

        // CORS
        app.UseCors("AllowFrontend");

        // Authentication & Authorization
        if (options.EnableAuthentication)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        // Controllers
        app.MapControllers();

        // Health checks
        if (options.EnableHealthChecks)
        {
            app.UseCustomHealthChecks();
        }

        return app;
    }
}
