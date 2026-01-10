namespace MyApp.Shared.Infrastructure.Messaging;

/// <summary>
/// Configuration options for event publishing
/// </summary>
public class EventPublisherOptions
{
    /// <summary>
    /// The name of the pub/sub component to use
    /// </summary>
    public string PubSubName { get; set; } = "pubsub";

    /// <summary>
    /// Whether to enable logging for event publishing operations
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
