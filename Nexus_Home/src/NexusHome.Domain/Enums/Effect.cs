namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission effect: grant or deny.
/// Resolved per DATABASE_MVP_SUMMARY.md - effect column with check constraint (grant/deny).
/// </summary>
public enum Effect
{
    /// <summary>
    /// Grants the permission.
    /// </summary>
    Grant,

    /// <summary>
    /// Denies the permission (explicit deny overrides grants).
    /// </summary>
    Deny
}