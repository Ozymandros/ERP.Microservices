using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Infrastructure.Messaging;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Messaging;

// Test event class for EventPublisher tests
public class TestEvent
{
    public string Message { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class EventPublisherTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly Mock<ILogger<EventPublisher>> _mockLogger;
    private readonly Mock<IOptions<EventPublisherOptions>> _mockOptions;
    private readonly EventPublisher _eventPublisher;

    public EventPublisherTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _mockLogger = new Mock<ILogger<EventPublisher>>();
        _mockOptions = new Mock<IOptions<EventPublisherOptions>>();

        _mockOptions.Setup(o => o.Value).Returns(new EventPublisherOptions
        {
            PubSubName = "test-pubsub",
            EnableLogging = true
        });

        _eventPublisher = new EventPublisher(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDaprClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventPublisher(
            null!,
            _mockLogger.Object,
            _mockOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventPublisher(
            _mockDaprClient.Object,
            null!,
            _mockOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventPublisher(
            _mockDaprClient.Object,
            _mockLogger.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var publisher = new EventPublisher(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        // Assert
        publisher.Should().NotBeNull();
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithValidEvent_PublishesEvent()
    {
        // Arrange
        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test", Value = 42 };
        var cancellationToken = CancellationToken.None;

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.Is<string>(s => s == "test-pubsub"),
                It.Is<string>(s => s == topic),
                It.Is<TestEvent>(e => e.Message == "Test"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(topic, eventData, cancellationToken);

        // Assert
        _mockDaprClient.Verify(
            c => c.PublishEventAsync(
                "test-pubsub",
                topic,
                eventData,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullTopic_ThrowsArgumentException()
    {
        // Arrange
        var eventData = new TestEvent { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventPublisher.PublishAsync(null!, eventData));
    }

    [Fact]
    public async Task PublishAsync_WithEmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        var eventData = new TestEvent { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventPublisher.PublishAsync(string.Empty, eventData));
    }

    [Fact]
    public async Task PublishAsync_WithWhitespaceTopic_ThrowsArgumentException()
    {
        // Arrange
        var eventData = new TestEvent { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _eventPublisher.PublishAsync("   ", eventData));
    }

    [Fact]
    public async Task PublishAsync_WithNullEventData_ThrowsArgumentNullException()
    {
        // Arrange
        var topic = "test-topic";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _eventPublisher.PublishAsync<TestEvent>(topic, null!));
    }

    [Fact]
    public async Task PublishAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test" };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _eventPublisher.PublishAsync(topic, eventData, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task PublishAsync_WhenDaprClientThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test" };
        var exception = new Exception("Dapr error");

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() =>
            _eventPublisher.PublishAsync(topic, eventData));

        thrownException.Should().Be(exception);

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithLoggingEnabled_LogsDebugAndTrace()
    {
        // Arrange
        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test", Value = 42 };

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(topic, eventData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithLoggingDisabled_DoesNotLog()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(new EventPublisherOptions
        {
            PubSubName = "test-pubsub",
            EnableLogging = false
        });

        var publisher = new EventPublisher(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test" };

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await publisher.PublishAsync(topic, eventData);

        // Assert - Verify no debug or trace logs
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithDifferentEventTypes_PublishesCorrectly()
    {
        // Arrange
        var topic = "test-topic";
        var stringEvent = "string-event";
        var intEvent = 42;
        var complexEvent = new TestEvent { Message = "Complex", Value = 100 };

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _eventPublisher.PublishAsync(topic, stringEvent);
        await _eventPublisher.PublishAsync(topic, intEvent);
        await _eventPublisher.PublishAsync(topic, complexEvent);

        // Assert
        _mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(e => e == stringEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<int>(e => e == intEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDaprClient.Verify(
            c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<TestEvent>(e => e.Message == "Complex"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_UsesCorrectPubSubName()
    {
        // Arrange
        var customPubSubName = "custom-pubsub";
        _mockOptions.Setup(o => o.Value).Returns(new EventPublisherOptions
        {
            PubSubName = customPubSubName,
            EnableLogging = false
        });

        var publisher = new EventPublisher(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var topic = "test-topic";
        var eventData = new TestEvent { Message = "Test" };

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await publisher.PublishAsync(topic, eventData);

        // Assert
        _mockDaprClient.Verify(
            c => c.PublishEventAsync(
                customPubSubName,
                topic,
                eventData,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
