using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Infrastructure.Caching;
using MyApp.Shared.Infrastructure.OpenApi;
using MyApp.Shared.Infrastructure.Repositories;
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
    /// Adds default microservice services to the service collection.
    /// Configures: Dapr, OpenTelemetry, Controllers, OpenAPI, Authentication, Database, Health Checks, CORS, AutoMapper.
    /// 
    /// Note: Redis cache must be configured separately before calling this method:
    ///   builder.AddRedisDistributedCache("cache");  // Aspire extension - must be called first
    ///   builder.AddServiceDefaults(...);
    /// This is because AddRedisDistributedCache requires the Aspire Redis resource reference,
    /// which is only available in the AppHost project context.
    /// </summary>
    public static WebApplicationBuilder AddServiceDefaults(
        this WebApplicationBuilder builder,
        MicroserviceConfigurationOptions? options = null)
    {
        options ??= new MicroserviceConfigurationOptions();

        // Auto-detect service name if not provided
        var serviceName = options.ServiceName
            ?? builder.Environment.ApplicationName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? "UnknownService";
        options.ServiceName = serviceName;

        // 1. DAPR Client and Messaging Services
        if (options.EnableDapr)
        {
            builder.Services.AddMicroserviceMessaging();
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

            // Use reflection to call AddDbContext<TContext> with proper parameters
            var addDbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods()
                .First(m => m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == 4); // Now looking for 4 parameters

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
            builder.Services.AddScoped<IPermissionChecker, PermissionChecker>();
        }

        // 9. Redis Distributed Cache
        // IMPORTANT: AddRedisDistributedCache("cache") must be called BEFORE AddServiceDefaults
        // by the API project (it's an Aspire extension method from Aspire.StackExchange.Redis.DistributedCaching
        // that requires the Redis resource reference from the AppHost).
        // We only register the ICacheService wrapper here; the actual Redis connection is configured by Aspire.
        if (options.EnableRedisCache)
        {
            builder.Services.AddScoped<ICacheService, DistributedCacheWrapper>();
        }

        // 10. AutoMapper (if enabled and assembly provided)
        if (options.EnableAutoMapper && options.AutoMapperAssembly != null)
        {
            // AutoMapper 16+ ships its own DI registration extensions (no separate package needed).
            builder.Services.AddAutoMapper(cfg => { }, options.AutoMapperAssembly);
        }

        // 11. CORS: any origin in dev; localhost-only in production
        builder.Services.AddAllowFrontendCors(builder.Configuration, builder.Environment);

        // 12. Unit of work (per service DbContext); override in ConfigureServiceDependencies if needed
        if (options.DbContextType != null)
        {
            builder.Services.AddScoped<IUnitOfWork>(sp =>
            {
                var dbContext = (DbContext)sp.GetRequiredService(options.DbContextType);
                return new EfUnitOfWork(dbContext);
            });
        }

        // 13. Service-specific dependencies
        options.ConfigureServiceDependencies?.Invoke(builder.Services);

        // Store options in DI for UseServiceDefaults to reuse (avoids passing options twice)
        builder.Services.Configure<MicroserviceConfigurationOptions>(opt =>
        {
            opt.ServiceName = options.ServiceName;
            opt.ConnectionStringKey = options.ConnectionStringKey;
            opt.DbContextType = options.DbContextType;
            opt.EnableAuthentication = options.EnableAuthentication;
            opt.EnableHealthChecks = options.EnableHealthChecks;
            opt.EnableOpenTelemetry = options.EnableOpenTelemetry;
            opt.EnableDapr = options.EnableDapr;
            opt.EnableRedisCache = options.EnableRedisCache;
            opt.EnableAutoMapper = options.EnableAutoMapper;
            opt.AutoMapperAssembly = options.AutoMapperAssembly;
            opt.RedisConnectionName = options.RedisConnectionName;
            opt.ConfigureServiceDependencies = options.ConfigureServiceDependencies;
        });

        return builder;
    }

    /// <summary>
    /// Applies default microservice middleware pipeline.
    /// Configures: Database migrations, OpenAPI, Scalar docs (dev), HTTPS redirect, Routing, CORS,
    /// Authentication, Authorization, Controllers, Health checks, Dapr pub/sub subscriptions.
    /// 
    /// If options are not provided, automatically reuses options from AddServiceDefaults via DI.
    /// This means you can call: app.UseServiceDefaults(); without passing options again.
    /// </summary>
    public static WebApplication UseServiceDefaults(
        this WebApplication app,
        MicroserviceConfigurationOptions? options = null)
    {
        // Try to reuse options from AddServiceDefaults via DI if not provided
        // NOTE: Use IOptionsMonitor<T> instead of IOptionsSnapshot<T> because IOptionsSnapshot is scoped
        // and cannot be resolved from the root provider (app.Services) during startup
        if (options == null)
        {
            try
            {
                var configMonitor = app.Services.GetService<IOptionsMonitor<MicroserviceConfigurationOptions>>();
                options = configMonitor?.CurrentValue ?? new MicroserviceConfigurationOptions();
            }
            catch (InvalidOperationException)
            {
                // Options not registered in DI, use defaults
                options = new MicroserviceConfigurationOptions();
            }
        }

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

        // Dapr invokes over HTTP; HTTPS redirection breaks sidecar calls in local dev.
        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        // Routing
        app.UseRouting();

        // Dapr pub/sub delivers CloudEvents; unwrap before model binding on [Topic] handlers.
        if (options.EnableDapr)
            app.UseCloudEvents();

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

        // Registers /dapr/subscribe and wires [Topic] endpoints to the Dapr sidecar
        if (options.EnableDapr)
            app.MapSubscribeHandler();

        // Health checks
        if (options.EnableHealthChecks)
            app.UseCustomHealthChecks();

        return app;
    }
}
