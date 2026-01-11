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

    public EventPublisher(
        DaprClient daprClient,
        ILogger<EventPublisher> logger,
        IOptions<EventPublisherOptions> options)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or empty", nameof(topic));

        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        try
        {
            if (_options.EnableLogging)
            {
                _logger.LogDebug(
                    "Publishing event to topic '{Topic}' on pubsub '{PubSubName}': {EventType}",
                    topic,
                    _options.PubSubName,
                    typeof(TEvent).Name);
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
                    "Successfully published event to topic '{Topic}': {EventType}",
                    topic,
                    typeof(TEvent).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event to topic '{Topic}' on pubsub '{PubSubName}': {EventType}",
                topic,
                _options.PubSubName,
                typeof(TEvent).Name);
            throw;
        }
    }
}
