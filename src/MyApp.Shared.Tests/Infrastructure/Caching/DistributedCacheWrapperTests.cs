using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MyApp.Shared.Infrastructure.Caching;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Caching;

// Test class for cache serialization
public class TestCacheObject
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DistributedCacheWrapperTests
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly DistributedCacheWrapper _cacheWrapper;

    public DistributedCacheWrapperTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _cacheWrapper = new DistributedCacheWrapper(_mockDistributedCache.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDistributedCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DistributedCacheWrapper(null!));
    }

    [Fact]
    public void Constructor_WithValidDistributedCache_CreatesInstance()
    {
        // Act
        var wrapper = new DistributedCacheWrapper(_mockDistributedCache.Object);

        // Assert
        wrapper.Should().NotBeNull();
    }

    #endregion

    #region GetStateAsync Tests

    [Fact]
    public async Task GetStateAsync_WithExistingKey_ReturnsDeserializedObject()
    {
        // Arrange
        var key = "test-key";
        var testObject = new TestCacheObject { Name = "Test", Value = 42, Timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(testObject);
        var bytes = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        // Act
        var result = await _cacheWrapper.GetStateAsync<TestCacheObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetStateAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheWrapper.GetStateAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStateAsync_WithInvalidJson_RemovesKeyAndReturnsNull()
    {
        // Arrange
        var key = "invalid-key";
        var invalidBytes = Encoding.UTF8.GetBytes("invalid json");

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidBytes);

        _mockDistributedCache
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _cacheWrapper.GetStateAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
        _mockDistributedCache.Verify(
            c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStateAsync_WithEmptyBytes_ReturnsNull()
    {
        // Arrange
        var key = "empty-key";
        var emptyBytes = Array.Empty<byte>();

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyBytes);

        // Act
        var result = await _cacheWrapper.GetStateAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStateAsync_WithDifferentTypes_DeserializesCorrectly()
    {
        // Arrange
        var stringKey = "string-key";
        var stringValue = "test-string";
        var stringBytes = Encoding.UTF8.GetBytes($"\"{stringValue}\"");

        var objectKey = "object-key";
        var testObject = new TestCacheObject { Name = "Test", Value = 42 };
        var objectJson = JsonSerializer.Serialize(testObject);
        var objectBytes = Encoding.UTF8.GetBytes(objectJson);

        _mockDistributedCache
            .Setup(c => c.GetAsync(stringKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stringBytes);

        _mockDistributedCache
            .Setup(c => c.GetAsync(objectKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objectBytes);

        // Act
        var stringResult = await _cacheWrapper.GetStateAsync<string>(stringKey);
        var objectResult = await _cacheWrapper.GetStateAsync<TestCacheObject>(objectKey);

        // Assert
        stringResult.Should().Be(stringValue);
        objectResult.Should().NotBeNull();
        objectResult!.Name.Should().Be("Test");
        objectResult.Value.Should().Be(42);
    }

    #endregion

    #region SaveStateAsync Tests

    [Fact]
    public async Task SaveStateAsync_WithValidObject_SavesToCache()
    {
        // Arrange
        var key = "save-key";
        var testObject = new TestCacheObject { Name = "Test", Value = 42, Timestamp = DateTime.UtcNow };
        var expiration = TimeSpan.FromMinutes(30);

        _mockDistributedCache
            .Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.SaveStateAsync(key, testObject, expiration);

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetAsync(
                key,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("Test", StringComparison.OrdinalIgnoreCase)),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveStateAsync_WithNullExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var key = "save-key";
        var testObject = new TestCacheObject { Name = "Test", Value = 42 };
        var defaultExpiration = TimeSpan.FromHours(1);

        _mockDistributedCache
            .Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == defaultExpiration),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.SaveStateAsync(key, testObject, null);

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == defaultExpiration),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveStateAsync_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var key = "complex-key";
        var testObject = new TestCacheObject
        {
            Name = "Complex",
            Value = 100,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        byte[]? capturedBytes = null;
        _mockDistributedCache
            .Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, b, o, ct) => capturedBytes = b)
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.SaveStateAsync(key, testObject);

        // Assert
        capturedBytes.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedBytes!);
        var deserialized = JsonSerializer.Deserialize<TestCacheObject>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Complex");
        deserialized.Value.Should().Be(100);
    }

    [Fact]
    public async Task SaveStateAsync_WithZeroExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var key = "zero-expiration-key";
        var testObject = new TestCacheObject { Name = "Test", Value = 42 };
        var zeroExpiration = TimeSpan.Zero;

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockDistributedCache
            .Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, b, o, ct) => capturedOptions = o)
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.SaveStateAsync(key, testObject, zeroExpiration);

        // Assert
        _mockDistributedCache.Verify(
            c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // DistributedCacheEntryOptions doesn't accept TimeSpan.Zero, so it uses default (1 hour)
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromHours(1));
    }

    #endregion

    #region RemoveStateAsync Tests

    [Fact]
    public async Task RemoveStateAsync_WithValidKey_RemovesFromCache()
    {
        // Arrange
        var key = "remove-key";

        _mockDistributedCache
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.RemoveStateAsync(key);

        // Assert
        _mockDistributedCache.Verify(
            c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveStateAsync_WithNonExistentKey_CompletesSuccessfully()
    {
        // Arrange
        var key = "non-existent-key";

        _mockDistributedCache
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheWrapper.RemoveStateAsync(key);

        // Assert
        _mockDistributedCache.Verify(
            c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SaveAndGetStateAsync_RoundTrip_WorksCorrectly()
    {
        // Arrange
        var key = "roundtrip-key";
        var originalObject = new TestCacheObject { Name = "RoundTrip", Value = 99, Timestamp = DateTime.UtcNow };

        byte[]? savedBytes = null;
        _mockDistributedCache
            .Setup(c => c.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, b, o, ct) => savedBytes = b)
            .Returns(Task.CompletedTask);

        _mockDistributedCache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedBytes);

        // Act
        await _cacheWrapper.SaveStateAsync(key, originalObject);
        var retrievedObject = await _cacheWrapper.GetStateAsync<TestCacheObject>(key);

        // Assert
        retrievedObject.Should().NotBeNull();
        retrievedObject!.Name.Should().Be(originalObject.Name);
        retrievedObject.Value.Should().Be(originalObject.Value);
    }

    #endregion
}
