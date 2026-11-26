namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission type values.
/// BLOCKER B-01: These values are proposed per v1.5 baseline but NOT formally approved.
/// Product + Security must approve exact enum values for permission_type_enum PostgreSQL type.
/// PostgreSQL enum label mapping is configured in Infrastructure layer via value converters.
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// Control device (e.g., turn on/off, set temperature).
    /// PG Enum Label: "device:control"
    /// </summary>
    DeviceControl,

    /// <summary>
    /// View device state (read-only).
    /// PG Enum Label: "device:view"
    /// </summary>
    DeviceView,

    /// <summary>
    /// Manage device (add/remove/configure).
    /// PG Enum Label: "device:manage"
    /// </summary>
    DeviceManage,

    /// <summary>
    /// Manage home (settings, members, rooms).
    /// PG Enum Label: "home:manage"
    /// </summary>
    HomeManage,

    /// <summary>
    /// Manage members (invite/remove/change roles).
    /// PG Enum Label: "member:manage"
    /// </summary>
    MemberManage,

    /// <summary>
    /// Stream camera (live video).
    /// Privacy-sensitive.
    /// PG Enum Label: "camera:stream"
    /// </summary>
    CameraStream,

    /// <summary>
    /// View camera (snapshots, recorded).
    /// Privacy-sensitive.
    /// PG Enum Label: "camera:view"
    /// </summary>
    CameraView,

    /// <summary>
    /// Stream microphone (live audio).
    /// Privacy-sensitive.
    /// PG Enum Label: "microphone:stream"
    /// </summary>
    MicrophoneStream,

    /// <summary>
    /// Manage settings.
    /// PG Enum Label: "settings:manage"
    /// </summary>
    SettingsManage,

    /// <summary>
    /// Propose AI actions.
    /// PG Enum Label: "ai:propose"
    /// </summary>
    AiPropose,

    /// <summary>
    /// Execute AI actions.
    /// PG Enum Label: "ai:execute"
    /// </summary>
    AiExecute
}

/// <summary>
/// Extension methods for PermissionType.
/// </summary>
public static class PermissionTypeExtensions
{
    /// <summary>
    /// All permission types in the catalog.
    /// </summary>
    public static readonly IReadOnlyList<PermissionType> All = new[]
    {
        PermissionType.DeviceControl, PermissionType.DeviceView, PermissionType.DeviceManage,
        PermissionType.HomeManage, PermissionType.MemberManage,
        PermissionType.CameraStream, PermissionType.CameraView,
        PermissionType.MicrophoneStream,
        PermissionType.SettingsManage,
        PermissionType.AiPropose, PermissionType.AiExecute
    };

    /// <summary>
    /// Privacy-sensitive permission types (camera/microphone).
    /// These are never inferred from role defaults.
    /// </summary>
    public static readonly IReadOnlyList<PermissionType> PrivacySensitive = new[]
    {
        PermissionType.CameraStream, PermissionType.CameraView, PermissionType.MicrophoneStream
    };

    /// <summary>
    /// Owner default permission types (per v1.5 baseline).
    /// Member/Guest defaults are disabled pending B-03 approval.
    /// </summary>
    public static readonly IReadOnlyList<PermissionType> OwnerDefaults = new[]
    {
        PermissionType.DeviceControl, PermissionType.DeviceView, PermissionType.DeviceManage,
        PermissionType.HomeManage, PermissionType.MemberManage,
        PermissionType.SettingsManage,
        PermissionType.AiPropose, PermissionType.AiExecute
    };

    /// <summary>
    /// Checks if a permission type is privacy-sensitive.
    /// </summary>
    public static bool IsPrivacySensitive(this PermissionType permissionType)
        => PrivacySensitive.Contains(permissionType);

    /// <summary>
    /// Checks if a permission type is in Owner defaults.
    /// </summary>
    public static bool IsOwnerDefault(this PermissionType permissionType)
        => OwnerDefaults.Contains(permissionType);

    /// <summary>
    /// Tries to parse a PostgreSQL enum label (e.g., "device:control") to PermissionType.
    /// </summary>
    public static bool TryParseFromPgLabel(string pgLabel, out PermissionType permissionType)
    {
        return pgLabel switch
        {
            "device:control" => (permissionType = PermissionType.DeviceControl, true).Item2,
            "device:view" => (permissionType = PermissionType.DeviceView, true).Item2,
            "device:manage" => (permissionType = PermissionType.DeviceManage, true).Item2,
            "home:manage" => (permissionType = PermissionType.HomeManage, true).Item2,
            "member:manage" => (permissionType = PermissionType.MemberManage, true).Item2,
            "camera:stream" => (permissionType = PermissionType.CameraStream, true).Item2,
            "camera:view" => (permissionType = PermissionType.CameraView, true).Item2,
            "microphone:stream" => (permissionType = PermissionType.MicrophoneStream, true).Item2,
            "settings:manage" => (permissionType = PermissionType.SettingsManage, true).Item2,
            "ai:propose" => (permissionType = PermissionType.AiPropose, true).Item2,
            "ai:execute" => (permissionType = PermissionType.AiExecute, true).Item2,
            _ => Enum.TryParse<PermissionType>(pgLabel, true, out permissionType) // Fallback to C# enum name
        };
    }

    /// <summary>
    /// Gets the PostgreSQL enum label for a permission type.
    /// </summary>
    public static string ToPgLabel(this PermissionType permissionType)
        => permissionType switch
        {
            PermissionType.DeviceControl => "device:control",
            PermissionType.DeviceView => "device:view",
            PermissionType.DeviceManage => "device:manage",
            PermissionType.HomeManage => "home:manage",
            PermissionType.MemberManage => "member:manage",
            PermissionType.CameraStream => "camera:stream",
            PermissionType.CameraView => "camera:view",
            PermissionType.MicrophoneStream => "microphone:stream",
            PermissionType.SettingsManage => "settings:manage",
            PermissionType.AiPropose => "ai:propose",
            PermissionType.AiExecute => "ai:execute",
            _ => permissionType.ToString()
        };
}