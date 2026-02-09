using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.Messaging;
using System.Text.Json;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Extensions;

public class MessagingServiceExtensionsTests
{
    #region AddMicroserviceMessaging Tests

    [Fact]
    public void AddMicroserviceMessaging_RegistersDaprClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMicroserviceMessaging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var daprClient = serviceProvider.GetService<Dapr.Client.DaprClient>();
        daprClient.Should().NotBeNull();
    }

    [Fact]
    public void AddMicroserviceMessaging_RegistersIEventPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMicroserviceMessaging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventPublisher = serviceProvider.GetService<IEventPublisher>();
        eventPublisher.Should().NotBeNull();
        eventPublisher.Should().BeOfType<EventPublisher>();
    }

    [Fact]
    public void AddMicroserviceMessaging_RegistersIServiceInvoker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMicroserviceMessaging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var serviceInvoker = serviceProvider.GetService<IServiceInvoker>();
        serviceInvoker.Should().NotBeNull();
        serviceInvoker.Should().BeOfType<ServiceInvoker>();
    }

    [Fact]
    public void AddMicroserviceMessaging_RegistersJsonSerializerOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMicroserviceMessaging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var jsonOptions = serviceProvider.GetService<IOptions<JsonSerializerOptions>>();
        jsonOptions.Should().NotBeNull();
        jsonOptions!.Value.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        jsonOptions.Value.DefaultIgnoreCondition.Should().Be(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void AddMicroserviceMessaging_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMicroserviceMessaging(options =>
        {
            options.EventPublisher.PubSubName = "custom-pubsub";
            options.EventPublisher.EnableLogging = false;
            options.EnableServiceInvocationLogging = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventPublisherOptions = serviceProvider.GetService<IOptions<EventPublisherOptions>>();
        eventPublisherOptions.Should().NotBeNull();
        eventPublisherOptions!.Value.PubSubName.Should().Be("custom-pubsub");
        eventPublisherOptions.Value.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void AddMicroserviceMessaging_RegistersServicesAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMicroserviceMessaging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventPublisher1 = serviceProvider.GetService<IEventPublisher>();
        var eventPublisher2 = serviceProvider.GetService<IEventPublisher>();
        eventPublisher1.Should().BeSameAs(eventPublisher2);

        var serviceInvoker1 = serviceProvider.GetService<IServiceInvoker>();
        var serviceInvoker2 = serviceProvider.GetService<IServiceInvoker>();
        serviceInvoker1.Should().BeSameAs(serviceInvoker2);
    }

    #endregion

    #region AddEventPublisher Tests

    [Fact]
    public void AddEventPublisher_RegistersIEventPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEventPublisher();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var eventPublisher = serviceProvider.GetService<IEventPublisher>();
        eventPublisher.Should().NotBeNull();
        eventPublisher.Should().BeOfType<EventPublisher>();
    }

    [Fact]
    public void AddEventPublisher_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEventPublisher(options =>
        {
            options.PubSubName = "custom-pubsub";
            options.EnableLogging = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<EventPublisherOptions>>();
        options.Should().NotBeNull();
        options!.Value.PubSubName.Should().Be("custom-pubsub");
        options.Value.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void AddEventPublisher_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEventPublisher();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<EventPublisherOptions>>();
        options.Should().NotBeNull();
        options!.Value.PubSubName.Should().Be("pubsub"); // Default value
    }

    #endregion

    #region AddServiceInvoker Tests

    [Fact]
    public void AddServiceInvoker_RegistersIServiceInvoker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddServiceInvoker();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var serviceInvoker = serviceProvider.GetService<IServiceInvoker>();
        serviceInvoker.Should().NotBeNull();
        serviceInvoker.Should().BeOfType<ServiceInvoker>();
    }

    [Fact]
    public void AddServiceInvoker_WithLoggingDisabled_RegistersWithLoggingDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddServiceInvoker(enableLogging: false);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var serviceInvoker = serviceProvider.GetService<IServiceInvoker>();
        serviceInvoker.Should().NotBeNull();
    }

    [Fact]
    public void AddServiceInvoker_RegistersDaprClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServiceInvoker();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var daprClient = serviceProvider.GetService<Dapr.Client.DaprClient>();
        daprClient.Should().NotBeNull();
    }

    #endregion
}
