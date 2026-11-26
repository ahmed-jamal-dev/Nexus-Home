namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission target type values.
/// BLOCKER B-02: These values are proposed per v1.5 baseline but NOT formally approved.
/// Product + Security must approve exact enum values for target_type_enum PostgreSQL type.
/// PostgreSQL enum label mapping is configured in Infrastructure layer via value converters.
/// </summary>
public enum TargetType
{
    /// <summary>
    /// Device target.
    /// PG Enum Label: "device"
    /// </summary>
    Device,

    /// <summary>
    /// Home target.
    /// PG Enum Label: "home"
    /// </summary>
    Home,

    /// <summary>
    /// Room target.
    /// PG Enum Label: "room"
    /// </summary>
    Room,

    /// <summary>
    /// Camera target (logical - device with camera capability).
    /// PG Enum Label: "camera"
    /// </summary>
    Camera,

    /// <summary>
    /// Microphone target (logical - device with microphone capability).
    /// PG Enum Label: "microphone"
    /// </summary>
    Microphone
}

/// <summary>
/// Extension methods for TargetType.
/// </summary>
public static class TargetTypeExtensions
{
    /// <summary>
    /// All target types in the catalog.
    /// </summary>
    public static readonly IReadOnlyList<TargetType> All = new[]
    {
        TargetType.Device, TargetType.Home, TargetType.Room,
        TargetType.Camera, TargetType.Microphone
    };

    /// <summary>
    /// Tries to parse a PostgreSQL enum label (e.g., "device") to TargetType.
    /// </summary>
    public static bool TryParseFromPgLabel(string pgLabel, out TargetType targetType)
    {
        return pgLabel switch
        {
            "device" => (targetType = TargetType.Device, true).Item2,
            "home" => (targetType = TargetType.Home, true).Item2,
            "room" => (targetType = TargetType.Room, true).Item2,
            "camera" => (targetType = TargetType.Camera, true).Item2,
            "microphone" => (targetType = TargetType.Microphone, true).Item2,
            _ => Enum.TryParse<TargetType>(pgLabel, true, out targetType) // Fallback to C# enum name
        };
    }

    /// <summary>
    /// Gets the PostgreSQL enum label for a target type.
    /// </summary>
    public static string ToPgLabel(this TargetType targetType)
        => targetType switch
        {
            TargetType.Device => "device",
            TargetType.Home => "home",
            TargetType.Room => "room",
            TargetType.Camera => "camera",
            TargetType.Microphone => "microphone",
            _ => targetType.ToString()
        };
}