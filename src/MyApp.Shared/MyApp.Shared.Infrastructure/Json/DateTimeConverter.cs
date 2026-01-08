using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyApp.Shared.Infrastructure.Json;

/// <summary>
/// Custom JSON converter for DateTime that converts default(DateTime) to DateTime.UtcNow during serialization
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert default(DateTime) or DateTime.MinValue to DateTime.UtcNow during serialization
        // This prevents JsonException when OpenAPI schema generation tries to serialize default values
        var dateTimeToWrite = (value == default || value == DateTime.MinValue) ? DateTime.UtcNow : value;
        writer.WriteStringValue(dateTimeToWrite.ToString("o")); // ISO 8601 format
    }
}

