namespace NexusHome.Domain.Entities;

/// <summary>
/// Platform-level audit log for administrative actions.
/// </summary>
public sealed class PlatformAuditLog : Entity
{
    /// <summary>
    /// The user who performed the action.
    /// </summary>
    public Guid ActorUserId { get; private set; }

    /// <summary>
    /// Navigation to actor user.
    /// </summary>
    public User ActorUser { get; private set; } = null!;

    /// <summary>
    /// The action performed (e.g., "UserCreated", "RoleAssigned", "HomeDeleted").
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// The type of target entity (e.g., "User", "Home", "PlatformRole").
    /// </summary>
    public string TargetType { get; private set; } = string.Empty;

    /// <summary>
    /// The ID of the target entity.
    /// </summary>
    public Guid TargetId { get; private set; }

    /// <summary>
    /// Additional details about the action (JSON).
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// IP address of the actor (if available).
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent of the actor (if available).
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Whether the action succeeded.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private PlatformAuditLog() { } // EF Core

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public static PlatformAuditLog Create(Guid actorUserId, string action, string targetType, Guid targetId, string? details = null, string? ipAddress = null, string? userAgent = null, bool success = true, string? errorMessage = null)
    {
        var log = new PlatformAuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            ErrorMessage = errorMessage,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return log;
    }
}