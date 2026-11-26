namespace NexusHome.Application.Common.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;
using NexusHome.Domain.Enums;

/// <summary>
/// JSON converter for PermissionType that serializes to PostgreSQL enum labels (e.g., "device:control").
/// </summary>
public class PermissionTypeJsonConverter : JsonConverter<PermissionType>
{
    public override PermissionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("PermissionType cannot be null or empty");
        }

        if (PermissionTypeExtensions.TryParseFromPgLabel(value, out var permissionType))
        {
            return permissionType;
        }

        throw new JsonException($"Invalid PermissionType value: {value}");
    }

    public override void Write(Utf8JsonWriter writer, PermissionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToPgLabel());
    }
}