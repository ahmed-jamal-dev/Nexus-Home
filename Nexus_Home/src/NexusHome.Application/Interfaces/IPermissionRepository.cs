namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// Repository interface for Permission entity.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Adds a new permission.
    /// </summary>
    Task AddAsync(Permission permission, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    Task UpdateAsync(Permission permission, CancellationToken ct = default);

    /// <summary>
    /// Gets a permission by ID.
    /// </summary>
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a member has a specific permission on a target.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a member has a specific permission on a target with deny override.
    /// Returns true if granted and not denied.
    /// </summary>
    Task<bool> HasPermissionEffectiveAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists permissions for a grantee membership.
    /// </summary>
    Task<List<Permission>> ListByGranteeAsync(Guid granteeMembershipId, CancellationToken ct = default);

    /// <summary>
    /// Lists permissions for a home.
    /// </summary>
    Task<List<Permission>> ListByHomeAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Lists permissions for a specific target.
    /// </summary>
    Task<List<Permission>> ListByTargetAsync(TargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>
    /// Lists permissions for a home with grantee and grantor memberships loaded.
    /// </summary>
    Task<List<Permission>> ListByHomeWithDetailsAsync(Guid homeId, Guid? granteeMembershipId, TargetType? targetType, Guid? targetId, string? cursor, int limit, CancellationToken ct = default);

    /// <summary>
    /// Deletes a permission (hard delete).
    /// </summary>
    Task DeleteAsync(Permission permission, CancellationToken ct = default);

    /// <summary>
    /// Checks if a permission grant already exists (for duplicate prevention).
    /// </summary>
    Task<bool> ExistsAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        Effect effect,
        CancellationToken ct = default);
}