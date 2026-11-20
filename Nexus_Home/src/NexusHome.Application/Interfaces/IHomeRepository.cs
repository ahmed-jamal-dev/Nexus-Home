namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for Home entity.
/// </summary>
public interface IHomeRepository
{
    /// <summary>
    /// Gets a home by ID.
    /// </summary>
    Task<Home?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a home by ID with owner membership loaded.
    /// </summary>
    Task<Home?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a new home.
    /// </summary>
    Task AddAsync(Home home, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing home.
    /// </summary>
    Task UpdateAsync(Home home, CancellationToken ct = default);

    /// <summary>
    /// Checks if a home exists and is not deleted.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a home has an active owner.
    /// </summary>
    Task<bool> HasActiveOwnerAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user owns a specific home.
    /// </summary>
    Task<bool> UserOwnsHomeAsync(Guid userId, Guid homeId, CancellationToken ct = default);
}