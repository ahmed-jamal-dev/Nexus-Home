namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for claim credentials (stored as Invitations with Role=Owner, InviteeEmail=null).
/// </summary>
public interface IClaimCredentialRepository
{
    /// <summary>
    /// Gets the pending claim credential for a home (for update - uses row lock via FOR UPDATE).
    /// </summary>
    Task<Invitation?> GetPendingForUpdateAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Gets the pending claim credential for a home with explicit FOR UPDATE row lock.
    /// Use this when you need to prevent concurrent modifications during the claim flow.
    /// </summary>
    Task<Invitation?> GetPendingForUpdateWithLockAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Gets a claim credential by its token hash.
    /// </summary>
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Checks if a valid (unaccepted, unexpired, unrevoked) claim credential exists for a home.
    /// </summary>
    Task<bool> ExistsValidCredentialAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new claim credential.
    /// </summary>
    Task AddAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing claim credential.
    /// </summary>
    Task UpdateAsync(Invitation invitation, CancellationToken ct = default);
}