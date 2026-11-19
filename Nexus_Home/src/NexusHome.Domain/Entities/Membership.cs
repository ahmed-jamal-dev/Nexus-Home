namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// User membership in a home with a specific role.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.4.
/// No DeletedAt - soft-suspend uses IsActive = false.
/// Partial unique index: at most one ACTIVE owner per home.
/// </summary>
public class Membership : Entity
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Home ID.
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// Role in this home.
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Whether this membership is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When the user joined the home.
    /// </summary>
    public DateTimeOffset JoinedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation to user.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Permissions where this membership is the grantor.
    /// </summary>
    public ICollection<Permission> GrantedPermissions { get; private set; } = new List<Permission>();

    /// <summary>
    /// Permissions where this membership is the grantee.
    /// </summary>
    public ICollection<Permission> ReceivedPermissions { get; private set; } = new List<Permission>();

    protected Membership() { }

    public Membership(Guid id, Guid userId, Guid homeId, Role role)
        : base(id)
    {
        UserId = userId;
        HomeId = homeId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Changes the role.
    /// </summary>
    public void ChangeRole(Role newRole)
    {
        Role = newRole;
        Touch();
    }

    /// <summary>
    /// Deactivates the membership (soft suspend).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Activates the membership.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }
}