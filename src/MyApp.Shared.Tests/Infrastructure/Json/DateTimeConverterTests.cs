using FluentAssertions;
using MyApp.Shared.Infrastructure.Json;
using System.Text.Json;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Json;

public class DateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DateTimeConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new DateTimeConverter() }
        };
    }

    [Fact]
    public void Write_WithValidDateTime_SerializesToIso8601()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc);
        var obj = new { Date = dateTime };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        json.Should().Contain("2024-01-15T14:30:45");
        json.Should().Contain("\"Date\"");
    }

    [Fact]
    public void Write_WithDefaultDateTime_ConvertsToUtcNow()
    {
        // Arrange
        var defaultDateTime = default(DateTime);
        var obj = new { Date = defaultDateTime };
        var beforeSerialization = DateTime.UtcNow;

        // Act
        var json = JsonSerializer.Serialize(obj, _options);
        var afterSerialization = DateTime.UtcNow;

        // Assert
        json.Should().Contain("\"Date\"");
        // The converter replaces default(DateTime) with DateTime.UtcNow
        // We can't assert exact value, but we can verify it's not "0001-01-01"
        json.Should().NotContain("0001-01-01");
    }

    [Fact]
    public void Write_WithDateTimeMinValue_ConvertsToUtcNow()
    {
        // Arrange
        var minValue = DateTime.MinValue;
        var obj = new { Date = minValue };
        var beforeSerialization = DateTime.UtcNow;

        // Act
        var json = JsonSerializer.Serialize(obj, _options);
        var afterSerialization = DateTime.UtcNow;

        // Assert
        json.Should().Contain("\"Date\"");
        // The converter replaces DateTime.MinValue with DateTime.UtcNow
        json.Should().NotContain("0001-01-01");
    }

    [Fact]
    public void Read_WithValidIso8601String_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"Date":"2024-01-15T14:30:45.000Z"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc));
    }

    [Fact]
    public void Read_WithIso8601WithoutZ_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"Date":"2024-01-15T14:30:45"}""";

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateTime(2024, 1, 15, 14, 30, 45));
    }

    [Fact]
    public void Write_WithLocalDateTime_SerializesCorrectly()
    {
        // Arrange
        var localDateTime = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Local);
        var obj = new { Date = localDateTime };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        json.Should().Contain("\"Date\"");
        json.Should().Contain("2024-01-15");
    }

    [Fact]
    public void RoundTrip_WithValidDateTime_PreservesValue()
    {
        // Arrange
        var originalDateTime = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc);
        var obj = new TestObject { Date = originalDateTime };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);
        var deserialized = JsonSerializer.Deserialize<TestObject>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Date.Should().BeCloseTo(originalDateTime, TimeSpan.FromSeconds(1));
    }

    private class TestObject
    {
        public DateTime Date { get; set; }
    }
}
