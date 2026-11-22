namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository for platform role operations.
/// </summary>
public interface IPlatformRoleRepository
{
    /// <summary>
    /// Gets a platform role by its key.
    /// </summary>
    Task<PlatformRole?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a platform role by ID.
    /// </summary>
    Task<PlatformRole?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all platform roles.
    /// </summary>
    Task<List<PlatformRole>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new platform role.
    /// </summary>
    Task AddAsync(PlatformRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a platform role.
    /// </summary>
    Task UpdateAsync(PlatformRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a platform role (system roles cannot be deleted).
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all platform roles with their active assignment counts.
    /// </summary>
    Task<List<(PlatformRole Role, int Count)>> GetRolesWithCountsAsync(CancellationToken cancellationToken = default);
}