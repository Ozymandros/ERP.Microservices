namespace MyApp.Shared.Infrastructure.Messaging;

/// <summary>
/// Combined configuration options for messaging services
/// </summary>
public class MessagingOptions
{
    /// <summary>
    /// Options for event publishing
    /// </summary>
    public EventPublisherOptions EventPublisher { get; set; } = new();

    /// <summary>
    /// Whether to enable logging for service invocation operations
    /// </summary>
    public bool EnableServiceInvocationLogging { get; set; } = true;
}
