namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission target type values.
/// <para>
/// <strong>BLOCKER B-02:</strong> These values are proposed per v1.3 baseline but NOT formally approved.
/// Product + Security must approve exact enum values for <c>target_type_enum</c> PostgreSQL type.
/// </para>
/// <para>
/// Using string constants to avoid locking in unresolved values.
/// When approved, replace with a proper enum and PostgreSQL enum type.
/// </para>
/// </summary>
public static class TargetType
{
    /// <summary>
    /// Device target.
    /// </summary>
    public const string Device = "device";

    /// <summary>
    /// Home target.
    /// </summary>
    public const string Home = "home";

    /// <summary>
    /// Room target.
    /// </summary>
    public const string Room = "room";

    /// <summary>
    /// Camera target.
    /// </summary>
    public const string Camera = "camera";

    /// <summary>
    /// Microphone target.
    /// </summary>
    public const string Microphone = "microphone";

    /// <summary>
    /// All proposed target types.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Device, Home, Room, Camera, Microphone
    };
}