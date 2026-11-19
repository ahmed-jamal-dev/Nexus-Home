namespace NexusHome.Domain.StringConstants;

/// <summary>
/// Permission target type constants.
/// BLOCKER B-02: target_type_enum requires Product+Security approval.
/// Using string constants until enum is approved.
/// Per v1.3 baseline Section 6.2.9.
/// </summary>
public static class PermissionTargetTypeConstants
{
    /// <summary>
    /// Target is a user (direct user grant).
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Target is a membership (role-based grant).
    /// </summary>
    public const string Membership = "membership";

    /// <summary>
    /// Target is a group (future: group-based grant).
    /// </summary>
    public const string Group = "group";
}