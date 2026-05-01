using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Infrastructure.Messaging;
using System.Text.Json;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Messaging;

// Test request/response classes
public class TestRequest
{
    public string Message { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class TestResponse
{
    public string Result { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ServiceInvokerTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly Mock<ILogger<ServiceInvoker>> _mockLogger;
    private readonly Mock<IOptions<JsonSerializerOptions>> _mockJsonOptions;
    private readonly ServiceInvoker _serviceInvoker;

    public ServiceInvokerTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _mockLogger = new Mock<ILogger<ServiceInvoker>>();
        _mockJsonOptions = new Mock<IOptions<JsonSerializerOptions>>();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _mockJsonOptions.Setup(o => o.Value).Returns(jsonOptions);

        _serviceInvoker = new ServiceInvoker(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockJsonOptions.Object,
            enableLogging: true);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDaprClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceInvoker(
            null!,
            _mockLogger.Object,
            _mockJsonOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceInvoker(
            _mockDaprClient.Object,
            null!,
            _mockJsonOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullJsonOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceInvoker(
            _mockDaprClient.Object,
            _mockLogger.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithNullJsonOptionsValue_UsesDefaultOptions()
    {
        // Arrange
        var nullOptions = new Mock<IOptions<JsonSerializerOptions>>();
        nullOptions.Setup(o => o.Value).Returns((JsonSerializerOptions)null!);

        // Act
        var invoker = new ServiceInvoker(
            _mockDaprClient.Object,
            _mockLogger.Object,
            nullOptions.Object);

        // Assert
        invoker.Should().NotBeNull();
    }

    #endregion

    #region InvokeAsync<TRequest, TResponse> Tests

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_WithRequestAndResponse_InvokesService()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // This test would require integration tests with a real DaprClient or a wrapper interface.
        // For unit tests, we focus on testing argument validation and CreateRequest method instead.
    }

    [Fact]
    public async Task InvokeAsync_WithNullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync<TestRequest, TestResponse>(
                null!,
                "/api/test",
                HttpMethod.Post,
                request));
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync<TestRequest, TestResponse>(
                string.Empty,
                "/api/test",
                HttpMethod.Post,
                request));
    }

    [Fact]
    public async Task InvokeAsync_WithNullMethodPath_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync<TestRequest, TestResponse>(
                "test-service",
                null!,
                HttpMethod.Post,
                request));
    }

    [Fact]
    public async Task InvokeAsync_WithNullHttpMethod_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _serviceInvoker.InvokeAsync<TestRequest, TestResponse>(
                "test-service",
                "/api/test",
                null!,
                request));
    }

    [Fact]
    public async Task InvokeAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new TestRequest { Message = "Test" };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _serviceInvoker.InvokeAsync<TestRequest, TestResponse>(
                "test-service",
                "/api/test",
                HttpMethod.Post,
                request,
                cancellationTokenSource.Token));
    }

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_WhenDaprClientThrows_LogsErrorAndRethrows()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // Error handling and logging would be tested via integration tests.
    }

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_WithLoggingEnabled_LogsInformationAndTrace()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // Logging behavior would be tested via integration tests.
    }

    #endregion

    #region InvokeAsync<TResponse> Tests

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_WithResponseOnly_InvokesService()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // This test would require integration tests with a real DaprClient or a wrapper interface.
    }

    [Fact]
    public async Task InvokeAsync_WithResponseOnly_WithNullServiceName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync<TestResponse>(
                null!,
                "/api/test",
                HttpMethod.Get));
    }

    [Fact]
    public async Task InvokeAsync_WithResponseOnly_WithNullMethodPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync<TestResponse>(
                "test-service",
                null!,
                HttpMethod.Get));
    }

    #endregion

    #region InvokeAsync (void) Tests

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_Void_WithValidParameters_CompletesSuccessfully()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // This test would require integration tests with a real DaprClient or a wrapper interface.
        // Argument validation is tested in other tests (InvokeAsync_Void_WithNullServiceName_ThrowsArgumentException, etc.)
    }

    [Fact]
    public async Task InvokeAsync_Void_WithNullServiceName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync(
                null!,
                "/api/test",
                HttpMethod.Delete));
    }

    [Fact]
    public async Task InvokeAsync_Void_WithNullMethodPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceInvoker.InvokeAsync(
                "test-service",
                null!,
                HttpMethod.Delete));
    }

    #endregion

    #region CreateRequest Tests

    [Fact]
    public void CreateRequest_WithValidParameters_CreatesRequest()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Post;
        var requestBody = new TestRequest { Message = "Test", Value = 42 };

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                methodPath))
            .Returns(requestMessage);

        // Act
        var result = _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod, requestBody);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(httpMethod);
    }

    [Fact]
    public void CreateRequest_WithQueryParameters_AddsQueryString()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Get;
        var queryParams = new Dictionary<string, string?>
        {
            { "param1", "value1" },
            { "param2", "value2" }
        };

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                It.Is<string>(s => s.Contains("param1", StringComparison.OrdinalIgnoreCase) && s.Contains("param2", StringComparison.OrdinalIgnoreCase))))
            .Returns(requestMessage);

        // Act
        var result = _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod, null, queryParams);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CreateRequest_WithGetMethodAndBody_LogsWarning()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Get;
        var requestBody = new TestRequest { Message = "Test" };

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                methodPath))
            .Returns(requestMessage);

        // Act
        _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod, requestBody);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void CreateRequest_WithPostMethodAndBody_SetsContent()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Post;
        var requestBody = new TestRequest { Message = "Test", Value = 42 };

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                methodPath))
            .Returns(requestMessage);

        // Act
        var result = _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod, requestBody);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNull();
    }

    [Fact]
    public void CreateRequest_WithNullServiceName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceInvoker.CreateRequest(null!, "/api/test", HttpMethod.Get));
    }

    [Fact]
    public void CreateRequest_WithNullMethodPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceInvoker.CreateRequest("test-service", null!, HttpMethod.Get));
    }

    [Fact]
    public void CreateRequest_WithNullHttpMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _serviceInvoker.CreateRequest("test-service", "/api/test", null!));
    }

    [Fact]
    public void CreateRequest_WithNoBodyAndNoQueryParams_CreatesRequest()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Get;

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                methodPath))
            .Returns(requestMessage);

        // Act
        var result = _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod);

        // Assert
        result.Should().NotBeNull();
        result.Method.Should().Be(httpMethod);
        result.Content.Should().BeNull();
    }

    [Fact]
    public void CreateRequest_WithEmptyQueryParams_DoesNotAddQueryString()
    {
        // Arrange
        var serviceName = "test-service";
        var methodPath = "/api/test";
        var httpMethod = HttpMethod.Get;
        var emptyQueryParams = new Dictionary<string, string?>();

        var requestMessage = new HttpRequestMessage(httpMethod, "http://test");
        _mockDaprClient
            .Setup(c => c.CreateInvokeMethodRequest(
                httpMethod,
                serviceName,
                methodPath))
            .Returns(requestMessage);

        // Act
        var result = _serviceInvoker.CreateRequest(serviceName, methodPath, httpMethod, null, emptyQueryParams);

        // Assert
        result.Should().NotBeNull();
        _mockDaprClient.Verify(
            c => c.CreateInvokeMethodRequest(httpMethod, serviceName, methodPath),
            Times.Once);
    }

    [Fact]
    public void CreateRequest_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceInvoker.CreateRequest(string.Empty, "/api/test", HttpMethod.Get));
    }

    [Fact]
    public void CreateRequest_WithWhitespaceServiceName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceInvoker.CreateRequest("   ", "/api/test", HttpMethod.Get));
    }

    [Fact]
    public void CreateRequest_WithEmptyMethodPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceInvoker.CreateRequest("test-service", string.Empty, HttpMethod.Get));
    }

    #endregion

    #region InvokeAsync<TResponse>(HttpRequestMessage) Tests

    [Fact]
    public async Task InvokeAsync_WithHttpRequestMessage_InvokesService()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://test");
        var expectedResponse = new TestResponse { Result = "Success" };

        _mockDaprClient
#pragma warning disable CS0618 // Dapr InvokeMethodAsync(HttpRequestMessage) is obsolete but tested intentionally
            .Setup(c => c.InvokeMethodAsync<TestResponse>(
                request,
                It.IsAny<CancellationToken>()))
#pragma warning restore CS0618
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _serviceInvoker.InvokeAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be("Success");
    }

    [Fact]
    public async Task InvokeAsync_WithNullHttpRequestMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _serviceInvoker.InvokeAsync<TestResponse>(null!));
    }

    [Fact]
    public async Task InvokeAsync_WithHttpRequestMessage_WhenDaprClientThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://test");
        var exception = new Exception("Dapr error");

        _mockDaprClient
#pragma warning disable CS0618 // Dapr InvokeMethodAsync(HttpRequestMessage) is obsolete but tested intentionally
            .Setup(c => c.InvokeMethodAsync<TestResponse>(
                request,
                It.IsAny<CancellationToken>()))
#pragma warning restore CS0618
            .ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() =>
            _serviceInvoker.InvokeAsync<TestResponse>(request));

        thrownException.Should().Be(exception);

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
    public async Task InvokeAsync_WithHttpRequestMessage_AndCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _serviceInvoker.InvokeAsync<TestResponse>(request, cts.Token));
    }

    [Fact]
    public async Task InvokeAsync_WithHttpRequestMessage_AndLoggingDisabled_DoesNotLog()
    {
        // Arrange - create invoker with logging disabled
        var invokerWithLoggingDisabled = new ServiceInvoker(
            _mockDaprClient.Object,
            _mockLogger.Object,
            _mockJsonOptions.Object,
            enableLogging: false);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://test");
        var expectedResponse = new TestResponse { Result = "Success" };

        _mockDaprClient
#pragma warning disable CS0618 // Dapr InvokeMethodAsync(HttpRequestMessage) is obsolete but tested intentionally
            .Setup(c => c.InvokeMethodAsync<TestResponse>(
                request,
                It.IsAny<CancellationToken>()))
#pragma warning restore CS0618
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await invokerWithLoggingDisabled.InvokeAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
    }

    #endregion

    #region Logging Disabled Tests

    [Fact(Skip = "DaprClient.InvokeMethodAsync is not virtual and cannot be mocked. This requires integration tests.")]
    public async Task InvokeAsync_WithLoggingDisabled_DoesNotLog()
    {
        // Note: DaprClient.InvokeMethodAsync methods are not virtual, so Moq cannot mock them.
        // Logging behavior when disabled would be tested via integration tests.
    }

    #endregion
}
