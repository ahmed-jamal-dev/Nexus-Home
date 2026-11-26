namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// Grant/deny permission with target scoping.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.11.
/// - effect: grant or deny (resolved)
/// - permission_type: BLOCKER B-01 (using enum with [PgName] mapping)
/// - target_type: BLOCKER B-02 (using enum with [PgName] mapping)
/// - Polymorphic target_id (no FK enforcement - MVP trade-off)
/// - Trigger validates: grantor/grantee same home, target home consistency,
///   camera/microphone require capability, owner can grant, non-owner needs member:manage.
/// </summary>
public class Permission : Entity
{
    /// <summary>
    /// Grantor membership ID (null for system DENY rows).
    /// </summary>
    public Guid? GrantorMembershipId { get; private set; }

    /// <summary>
    /// Grantee membership ID (required).
    /// </summary>
    public Guid GranteeMembershipId { get; private set; }

    /// <summary>
    /// Permission type (e.g., "device:control", "camera:stream").
    /// BLOCKER B-01: awaiting Product + Security approval for enum values.
    /// </summary>
    public PermissionType PermissionType { get; private set; }

    /// <summary>
    /// Target type (device, home, room, camera, microphone).
    /// BLOCKER B-02: awaiting Product + Security approval for enum values.
    /// </summary>
    public TargetType TargetType { get; private set; }

    /// <summary>
    /// Target ID (polymorphic - references device/home/room).
    /// </summary>
    public Guid TargetId { get; private set; }

    /// <summary>
    /// Effect: grant or deny.
    /// </summary>
    public Effect Effect { get; private set; } = Effect.Grant;

    /// <summary>
    /// Home ID (required - permission is scoped to a home).
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// When the permission was granted.
    /// </summary>
    public DateTimeOffset GrantedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Navigation to grantor membership.
    /// </summary>
    public Membership? GrantorMembership { get; private set; }

    /// <summary>
    /// Navigation to grantee membership.
    /// </summary>
    public Membership? GranteeMembership { get; private set; }

    protected Permission() { }

    public Permission(Guid id, Guid homeId, Guid? grantorMembershipId, Guid granteeMembershipId,
        PermissionType permissionType, TargetType targetType, Guid targetId, Effect effect = Effect.Grant)
        : base(id)
    {
        HomeId = homeId;
        GrantorMembershipId = grantorMembershipId;
        GranteeMembershipId = granteeMembershipId;
        PermissionType = permissionType;
        TargetType = targetType;
        TargetId = targetId;
        Effect = effect;
        GrantedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if this is a system DENY row (no grantor).
    /// </summary>
    public bool IsSystemDeny => GrantorMembershipId == null && Effect == Effect.Deny;

    /// <summary>
    /// Checks if this permission is a grant (not a system deny).
    /// </summary>
    public bool IsGrant => Effect == Effect.Grant;

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    public static Permission Create(
        Guid granteeMembershipId,
        Guid? grantorMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        Effect effect,
        Guid homeId)
    {
        return new Permission(
            Guid.NewGuid(),
            homeId,
            grantorMembershipId,
            granteeMembershipId,
            permissionType,
            targetType,
            targetId,
            effect);
    }
}