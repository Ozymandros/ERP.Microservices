namespace MyApp.Shared.Domain.Messaging;

/// <summary>
/// Abstraction for publishing domain events to a message broker
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the specified topic
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish</typeparam>
    /// <param name="topic">The topic name to publish to</param>
    /// <param name="eventData">The event data to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default);
}
