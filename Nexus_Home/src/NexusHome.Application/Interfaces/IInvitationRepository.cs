namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for member invitations (stored as Invitations with InviteeEmail set).
/// </summary>
public interface IInvitationRepository
{
    /// <summary>
    /// Gets a pending member invitation by its token hash.
    /// </summary>
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Gets a pending member invitation by its token hash with FOR UPDATE row lock.
    /// Use this when you need to prevent concurrent modifications during acceptance.
    /// </summary>
    Task<Invitation?> GetByTokenHashWithLockAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Checks if a pending invitation exists for the given email and home.
    /// </summary>
    Task<bool> ExistsPendingForEmailAsync(Guid homeId, string email, CancellationToken ct = default);

    /// <summary>
    /// Adds a new member invitation.
    /// </summary>
    Task AddAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing member invitation.
    /// </summary>
    Task UpdateAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>
    /// Lists pending invitations for a home (for potential future use).
    /// </summary>
    Task<List<Invitation>> ListPendingByHomeAsync(Guid homeId, CancellationToken ct = default);
}