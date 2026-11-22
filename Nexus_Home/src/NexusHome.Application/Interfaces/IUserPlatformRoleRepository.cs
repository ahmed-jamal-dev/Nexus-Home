namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository for user platform role operations.
/// </summary>
public interface IUserPlatformRoleRepository
{
    /// <summary>
    /// Gets all active platform role assignments for a user.
    /// </summary>
    Task<List<UserPlatformRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active platform role assignments for a user with role details.
    /// </summary>
    Task<List<UserPlatformRole>> GetByUserIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active platform role assignments for a user with a specific role key.
    /// </summary>
    Task<List<UserPlatformRole>> GetByUserIdAndRoleKeyAsync(Guid userId, string roleKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active platform role assignments for a user scoped to a specific home (for Technician).
    /// </summary>
    Task<List<UserPlatformRole>> GetByUserIdAndScopeAsync(Guid userId, string scopeType, Guid scopeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific platform role (optionally scoped).
    /// </summary>
    Task<bool> HasRoleAsync(Guid userId, string roleKey, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a platform role with hierarchy level >= minimum.
    /// </summary>
    Task<bool> HasRoleAtLeastAsync(Guid userId, int minimumHierarchyLevel, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a platform role to a user.
    /// </summary>
    Task AddAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user platform role assignment.
    /// </summary>
    Task UpdateAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes (soft deletes) a user platform role assignment.
    /// </summary>
    Task<bool> RevokeAsync(Guid userId, Guid platformRoleId, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with a specific platform role.
    /// </summary>
    Task<List<UserPlatformRole>> GetByRoleKeyAsync(string roleKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has PlatformAdmin or SuperAdmin role.
    /// </summary>
    Task<bool> HasPlatformAdminOrHigherAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets platform role counts for all roles.
    /// </summary>
    Task<Dictionary<string, int>> GetRoleCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with their platform roles for list view.
    /// </summary>
    Task<List<(Guid UserId, string Email, string FullName, bool IsActive, bool EmailVerified, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<string> RoleKeys)>> GetUsersWithRolesAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        string? cursor = null,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of users matching filter.
    /// </summary>
    Task<int> GetUsersCountAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active platform role assignments for multiple users with role details.
    /// </summary>
    Task<List<UserPlatformRole>> GetByUserIdsWithRolesAsync(List<Guid> userIds, CancellationToken cancellationToken = default);
}