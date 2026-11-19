namespace NexusHome.Domain.StringConstants;

/// <summary>
/// Permission type constants.
/// BLOCKER B-01: permission_type_enum requires Product+Security approval.
/// Using string constants until enum is approved.
/// Per v1.3 baseline Section 6.2.9.
/// </summary>
public static class PermissionTypeConstants
{
    /// <summary>
    /// Manage members (invite, remove, change roles).
    /// </summary>
    public const string MemberManage = "member:manage";

    /// <summary>
    /// View members.
    /// </summary>
    public const string MemberView = "member:view";

    /// <summary>
    /// Manage devices (add, remove, configure).
    /// </summary>
    public const string DeviceManage = "device:manage";

    /// <summary>
    /// Control devices (on/off, set state).
    /// </summary>
    public const string DeviceControl = "device:control";

    /// <summary>
    /// View device state.
    /// </summary>
    public const string DeviceView = "device:view";

    /// <summary>
    /// Manage rooms.
    /// </summary>
    public const string RoomManage = "room:manage";

    /// <summary>
    /// View rooms.
    /// </summary>
    public const string RoomView = "room:view";

    /// <summary>
    /// Manage automations.
    /// </summary>
    public const string AutomationManage = "automation:manage";

    /// <summary>
    /// View automations.
    /// </summary>
    public const string AutomationView = "automation:view";

    /// <summary>
    /// Manage settings.
    /// </summary>
    public const string SettingsManage = "settings:manage";

    /// <summary>
    /// View settings.
    /// </summary>
    public const string SettingsView = "settings:view";

    /// <summary>
    /// Access camera/microphone (high-sensitivity).
    /// </summary>
    public const string CameraAccess = "camera:access";

    /// <summary>
    /// View camera stream.
    /// </summary>
    public const string CameraView = "camera:view";

    /// <summary>
    /// Manage AI actions.
    /// </summary>
    public const string AIManage = "ai:manage";

    /// <summary>
    /// View AI audit trail.
    /// </summary>
    public const string AIView = "ai:view";

    /// <summary>
    /// All permissions (owner).
    /// </summary>
    public const string All = "*";
}