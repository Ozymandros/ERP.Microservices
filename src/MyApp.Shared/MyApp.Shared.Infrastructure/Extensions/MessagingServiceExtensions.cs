using Microsoft.Extensions.DependencyInjection;
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
        // Register DaprClient (required for implementations)
        services.AddDaprClient();

        // Configure options
        if (configure != null)
        {
            services.Configure<EventPublisherOptions>(options =>
            {
                var messagingOptions = new MessagingOptions();
                configure(messagingOptions);
                options.PubSubName = messagingOptions.EventPublisher.PubSubName;
                options.EnableLogging = messagingOptions.EventPublisher.EnableLogging;
            });
        }
        else
        {
            services.Configure<EventPublisherOptions>(options => { }); // Use defaults
        }

        // Register messaging services
        services.AddSingleton<IEventPublisher, EventPublisher>();
        
        // Register ServiceInvoker with logging configuration
        services.AddSingleton<IServiceInvoker>(sp =>
        {
            var daprClient = sp.GetRequiredService<Dapr.Client.DaprClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceInvoker>>();
            
            var messagingOptions = new MessagingOptions();
            configure?.Invoke(messagingOptions);
            
            return new ServiceInvoker(daprClient, logger, messagingOptions.EnableServiceInvocationLogging);
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
        services.AddDaprClient();

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
        services.AddDaprClient();

        services.AddSingleton<IServiceInvoker>(sp =>
        {
            var daprClient = sp.GetRequiredService<Dapr.Client.DaprClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceInvoker>>();
            return new ServiceInvoker(daprClient, logger, enableLogging);
        });

        return services;
    }
}
