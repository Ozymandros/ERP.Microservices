using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Messaging;
namespace MyApp.Shared.Infrastructure.Messaging;

/// <summary>
/// Dapr-based implementation of IEventPublisher
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<EventPublisher> _logger;
    private readonly EventPublisherOptions _options;

    /// <summary>
    /// Initializes a new instance of the EventPublisher class.
    /// </summary>
    /// <param name="daprClient">The dapr Client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options.</param>
    public EventPublisher(
        DaprClient daprClient,
        ILogger<EventPublisher> logger,
        IOptions<EventPublisherOptions> options)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _daprClient = daprClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>Publishes the given event data to the specified Dapr pub/sub topic.</summary>
    /// <typeparam name="TEvent">The type of the event payload.</typeparam>
    /// <param name="topic">The kebab-case topic name to publish to.</param>
    /// <param name="eventData">The event payload to publish.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="topic"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventData"/> is null.</exception>
    public async Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or empty", nameof(topic));

        ArgumentNullException.ThrowIfNull(eventData);

        try
        {
            if (_options.EnableLogging)
            {
                _logger.LogDebug(
                    "Publishing event: {@Event}",
                    new { Topic = topic, PubSubName = _options.PubSubName, EventType = typeof(TEvent).Name });
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _daprClient.PublishEventAsync(
                _options.PubSubName,
                topic,
                eventData,
                cancellationToken);

            if (_options.EnableLogging)
            {
                _logger.LogTrace(
                    "Successfully published event: {@Event}",
                    new { Topic = topic, EventType = typeof(TEvent).Name });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event: {@Event}",
                new { Topic = topic, PubSubName = _options.PubSubName, EventType = typeof(TEvent).Name });
            throw;
        }
    }
}
