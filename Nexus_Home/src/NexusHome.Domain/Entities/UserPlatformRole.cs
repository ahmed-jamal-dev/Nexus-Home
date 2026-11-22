namespace NexusHome.Domain.Entities;

/// <summary>
/// User-to-platform-role assignment with optional scope.
/// </summary>
public sealed class UserPlatformRole : Entity
{
    /// <summary>
    /// The user assigned this role.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation to user.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// The platform role assigned.
    /// </summary>
    public Guid PlatformRoleId { get; private set; }

    /// <summary>
    /// Navigation to platform role.
    /// </summary>
    public PlatformRole PlatformRole { get; private set; } = null!;

    /// <summary>
    /// Optional scope identifier (e.g., HomeId for Technician, null for global roles).
    /// </summary>
    public Guid? ScopeId { get; private set; }

    /// <summary>
    /// Type of scope (e.g., "Home" for Technician).
    /// </summary>
    public string? ScopeType { get; private set; }

    /// <summary>
    /// Who assigned this role.
    /// </summary>
    public Guid AssignedBy { get; private set; }

    /// <summary>
    /// When the assignment was made.
    /// </summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>
    /// When the assignment expires (null = never).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Whether this assignment is active.
    /// </summary>
    public bool IsActive { get; private set; }

    private UserPlatformRole() { } // EF Core

    /// <summary>
    /// Creates a new user-platform-role assignment.
    /// </summary>
    public static UserPlatformRole Create(Guid userId, Guid platformRoleId, Guid assignedBy, Guid? scopeId = null, string? scopeType = null, DateTimeOffset? expiresAt = null)
    {
        var assignment = new UserPlatformRole
        {
            UserId = userId,
            PlatformRoleId = platformRoleId,
            AssignedBy = assignedBy,
            ScopeId = scopeId,
            ScopeType = scopeType,
            ExpiresAt = expiresAt,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        return assignment;
    }

    /// <summary>
    /// Revokes (soft-deletes) this assignment.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Checks if the assignment is currently valid.
    /// </summary>
    public bool IsValid() => IsActive && (ExpiresAt == null || ExpiresAt > DateTimeOffset.UtcNow);
}