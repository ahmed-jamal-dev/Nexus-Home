namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission type values.
/// <para>
/// <strong>BLOCKER B-01:</strong> These values are proposed per v1.3 baseline but NOT formally approved.
/// Product + Security must approve exact enum values for <c>permission_type_enum</c> PostgreSQL type.
/// </para>
/// <para>
/// Using string constants to avoid locking in unresolved values.
/// When approved, replace with a proper enum and PostgreSQL enum type.
/// </para>
/// </summary>
public static class PermissionType
{
    /// <summary>
    /// Control device (e.g., turn on/off, set temperature).
    /// </summary>
    public const string DeviceControl = "device:control";

    /// <summary>
    /// View device state (read-only).
    /// </summary>
    public const string DeviceView = "device:view";

    /// <summary>
    /// Manage device (add/remove/configure).
    /// </summary>
    public const string DeviceManage = "device:manage";

    /// <summary>
    /// Manage home (settings, members, rooms).
    /// </summary>
    public const string HomeManage = "home:manage";

    /// <summary>
    /// Manage members (invite/remove/change roles).
    /// </summary>
    public const string MemberManage = "member:manage";

    /// <summary>
    /// Stream camera (live video).
    /// </summary>
    public const string CameraStream = "camera:stream";

    /// <summary>
    /// View camera (snapshots, recorded).
    /// </summary>
    public const string CameraView = "camera:view";

    /// <summary>
    /// Stream microphone (live audio).
    /// </summary>
    public const string MicrophoneStream = "microphone:stream";

    /// <summary>
    /// Manage settings.
    /// </summary>
    public const string SettingsManage = "settings:manage";

    /// <summary>
    /// Propose AI actions.
    /// </summary>
    public const string AiPropose = "ai:propose";

    /// <summary>
    /// Execute AI actions.
    /// </summary>
    public const string AiExecute = "ai:execute";

    /// <summary>
    /// All proposed permission types.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        DeviceControl, DeviceView, DeviceManage,
        HomeManage, MemberManage,
        CameraStream, CameraView,
        MicrophoneStream,
        SettingsManage,
        AiPropose, AiExecute
    };

    /// <summary>
    /// Privacy-sensitive permission types (camera/microphone).
    /// These are never inferred from role defaults.
    /// </summary>
    public static readonly IReadOnlyList<string> PrivacySensitive = new[]
    {
        CameraStream, CameraView, MicrophoneStream
    };

    /// <summary>
    /// Owner default permission types (per v1.3 baseline Section 9.5).
    /// Member/Guest defaults are disabled pending B-03 approval.
    /// </summary>
    public static readonly IReadOnlyList<string> OwnerDefaults = new[]
    {
        DeviceControl, DeviceView, DeviceManage,
        HomeManage, MemberManage,
        SettingsManage,
        AiPropose, AiExecute
    };
}