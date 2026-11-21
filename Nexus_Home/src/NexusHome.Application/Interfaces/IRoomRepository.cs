namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for Room entity.
/// </summary>
public interface IRoomRepository
{
    /// <summary>
    /// Adds a new room.
    /// </summary>
    Task AddAsync(Room room, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing room.
    /// </summary>
    Task UpdateAsync(Room room, CancellationToken ct = default);

    /// <summary>
    /// Gets a room by ID.
    /// </summary>
    Task<Room?> GetByIdAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>
    /// Gets a room by ID with home loaded for authorization.
    /// </summary>
    Task<Room?> GetByIdWithHomeAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>
    /// Lists rooms for a home with cursor-based pagination.
    /// </summary>
    Task<List<Room>> ListAsync(Guid homeId, string? cursor, int limit, CancellationToken ct = default);

    /// <summary>
    /// Checks if a room with the given name exists in the home (among non-deleted).
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid homeId, string name, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a room by setting DeletedAt.
    /// </summary>
    Task SoftDeleteAsync(Guid roomId, CancellationToken ct = default);
}