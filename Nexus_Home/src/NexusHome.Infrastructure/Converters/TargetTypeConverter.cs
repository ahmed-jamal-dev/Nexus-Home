namespace NexusHome.Infrastructure.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusHome.Domain.Enums;

/// <summary>
/// Value converter for TargetType enum <-> PostgreSQL enum label strings.
/// Maps C# enum members to their corresponding PostgreSQL enum labels (e.g., Device -> "device").
/// </summary>
public class TargetTypeConverter : ValueConverter<TargetType, string>
{
    private static readonly Dictionary<TargetType, string> _toProvider = new()
    {
        { TargetType.Device, "device" },
        { TargetType.Home, "home" },
        { TargetType.Room, "room" },
        { TargetType.Camera, "camera" },
        { TargetType.Microphone, "microphone" }
    };

    private static readonly Dictionary<string, TargetType> _fromProvider = _toProvider
        .ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    public TargetTypeConverter()
        : base(
            v => ConvertToProvider(v),
            v => ConvertFromProvider(v))
    {
    }

    private static new string ConvertToProvider(TargetType v) => _toProvider.TryGetValue(v, out var s) ? s : v.ToString().ToLowerInvariant();

    private static new TargetType ConvertFromProvider(string v) => _fromProvider.TryGetValue(v, out var e) ? e : Enum.Parse<TargetType>(v, true);
}