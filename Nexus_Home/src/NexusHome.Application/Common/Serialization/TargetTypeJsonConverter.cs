namespace NexusHome.Application.Common.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;
using NexusHome.Domain.Enums;

/// <summary>
/// JSON converter for TargetType that serializes to PostgreSQL enum labels (e.g., "device").
/// </summary>
public class TargetTypeJsonConverter : JsonConverter<TargetType>
{
    public override TargetType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("TargetType cannot be null or empty");
        }

        if (TargetTypeExtensions.TryParseFromPgLabel(value, out var targetType))
        {
            return targetType;
        }

        throw new JsonException($"Invalid TargetType value: {value}");
    }

    public override void Write(Utf8JsonWriter writer, TargetType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToPgLabel());
    }
}