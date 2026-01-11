using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.AspNetCore;
using Dapr.Client;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Infrastructure.Messaging;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering messaging services
/// </summary>
public static class MessagingServiceExtensions
{
    /// <summary>
    /// Adds microservice messaging services (event publishing and service invocation)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMicroserviceMessaging(
        this IServiceCollection services,
        Action<MessagingOptions>? configure = null)
    {
        // Ensure Options services are registered (Configure<T> does this automatically, but explicit for clarity)
        services.AddOptions();

        // Configure JsonSerializerOptions for DaprClient and register in DI
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // Register DaprClient with consistent JSON serialization options
        // Use DaprClientBuilder to configure JsonSerializerOptions
        // Note: AddDaprClient from Dapr.AspNetCore accepts Action<DaprClientBuilder>?
        services.AddDaprClient(builder => builder.UseJsonSerializationOptions(jsonOptions));

        // Register the same JsonSerializerOptions in DI for use in ServiceInvoker.CreateRequest
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
            options.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
            options.WriteIndented = jsonOptions.WriteIndented;
        });

        // Store MessagingOptions configuration
        // Create and configure MessagingOptions during registration to avoid DI resolution issues
        // from root provider in singleton factory
        var messagingOptions = new MessagingOptions();
        if (configure != null)
        {
            configure(messagingOptions);
        }

        // Store MessagingOptions in DI for potential use by other services
        // This automatically registers IOptions<MessagingOptions>, IOptionsSnapshot<MessagingOptions>, and IOptionsMonitor<MessagingOptions>
        services.Configure<MessagingOptions>(options =>
        {
            // Apply the same configuration to the options stored in DI
            if (configure != null)
            {
                configure(options);
            }
        });

        // Configure EventPublisherOptions with values from MessagingOptions
        services.Configure<EventPublisherOptions>(options =>
        {
            options.PubSubName = messagingOptions.EventPublisher.PubSubName;
            options.EnableLogging = messagingOptions.EventPublisher.EnableLogging;
        });

        // Register messaging services
        services.AddSingleton<IEventPublisher, EventPublisher>();

        // Register ServiceInvoker - use captured options value to avoid DI resolution from root provider
        // This approach avoids the "Cannot resolve scoped service from root provider" error entirely
        // by capturing the options value during registration instead of resolving from DI at runtime
        var enableServiceInvocationLogging = messagingOptions.EnableServiceInvocationLogging;
        services.AddSingleton<IServiceInvoker>(sp =>
        {
            var daprClient = sp.GetRequiredService<Dapr.Client.DaprClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceInvoker>>();
            var jsonOptions = sp.GetRequiredService<IOptions<System.Text.Json.JsonSerializerOptions>>();

            return new ServiceInvoker(daprClient, logger, jsonOptions, enableServiceInvocationLogging);
        });

        return services;
    }

    /// <summary>
    /// Adds event publishing service only
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEventPublisher(
        this IServiceCollection services,
        Action<EventPublisherOptions>? configure = null)
    {
        // Configure JsonSerializerOptions for DaprClient
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // Register DaprClient with consistent JSON serialization options
        services.AddDaprClient(builder => builder.UseJsonSerializationOptions(jsonOptions));

        // Register JsonSerializerOptions in DI for ServiceInvoker
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
            options.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
            options.WriteIndented = jsonOptions.WriteIndented;
        });

        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<EventPublisherOptions>(options => { });
        }

        services.AddSingleton<IEventPublisher, EventPublisher>();

        return services;
    }

    /// <summary>
    /// Adds service invocation service only
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="enableLogging">Whether to enable logging</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddServiceInvoker(
        this IServiceCollection services,
        bool enableLogging = true)
    {
        // Configure JsonSerializerOptions for DaprClient
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // Register DaprClient with consistent JSON serialization options
        services.AddDaprClient(builder => builder.UseJsonSerializationOptions(jsonOptions));

        // Register JsonSerializerOptions in DI for ServiceInvoker
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
            options.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
            options.WriteIndented = jsonOptions.WriteIndented;
        });

        services.AddSingleton<IServiceInvoker>(sp =>
        {
            var daprClient = sp.GetRequiredService<Dapr.Client.DaprClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceInvoker>>();
            var jsonOptions = sp.GetRequiredService<IOptions<System.Text.Json.JsonSerializerOptions>>();
            return new ServiceInvoker(daprClient, logger, jsonOptions, enableLogging);
        });

        return services;
    }
}
