namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for Membership entity.
/// </summary>
public interface IMembershipRepository
{
    /// <summary>
    /// Adds a new membership.
    /// </summary>
    Task AddAsync(Membership membership, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing membership.
    /// </summary>
    Task UpdateAsync(Membership membership, CancellationToken ct = default);

    /// <summary>
    /// Gets a membership by ID.
    /// </summary>
    Task<Membership?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the active owner membership for a home.
    /// </summary>
    Task<Membership?> GetActiveOwnerAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user is an active member of a home.
    /// </summary>
    Task<bool> IsActiveMemberAsync(Guid userId, Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user is an active owner of a home.
    /// </summary>
    Task<bool> IsActiveOwnerAsync(Guid userId, Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Lists active memberships for a home with cursor-based pagination.
    /// </summary>
    Task<List<Membership>> ListAsync(Guid homeId, string? cursor, int limit, CancellationToken ct = default);

/// <summary>
/// Attempts to acquire a transaction-scoped advisory lock for last-owner protection on a home.
/// Returns true if lock acquired, false if already held by another transaction.
/// </summary>
Task<bool> TryAcquireLastOwnerLockAsync(Guid homeId, CancellationToken ct = default);

/// <summary>
/// Releases the advisory lock for last-owner protection.
/// No-op for transaction-scoped locks (auto-released on commit/rollback); kept for interface symmetry.
/// </summary>
Task ReleaseLastOwnerLockAsync(Guid homeId, CancellationToken ct = default);
}