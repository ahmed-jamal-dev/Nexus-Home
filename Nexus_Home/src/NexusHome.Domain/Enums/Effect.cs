namespace NexusHome.Domain.Enums;

/// <summary>
/// Permission effect: grant or deny.
/// Resolved per DATABASE_MVP_SUMMARY.md - effect column with check constraint (grant/deny).
/// PostgreSQL enum label mapping is configured in Infrastructure layer via value converters.
/// </summary>
public enum Effect
{
    /// <summary>
    /// Grants the permission.
    /// PG Enum Label: "Grant"
    /// </summary>
    Grant,

    /// <summary>
    /// Denies the permission (explicit deny overrides grants).
    /// PG Enum Label: "Deny"
    /// </summary>
    Deny
}