namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for Device entity.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Adds a new device.
    /// </summary>
    Task AddAsync(Device device, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing device.
    /// </summary>
    Task UpdateAsync(Device device, CancellationToken ct = default);

    /// <summary>
    /// Gets a device by ID.
    /// </summary>
    Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Gets a device by ID with home loaded for authorization.
    /// </summary>
    Task<Device?> GetByIdWithHomeAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Gets a device by external ID.
    /// </summary>
    Task<Device?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Lists devices for a home with cursor-based pagination, optionally filtered by room.
    /// </summary>
    Task<List<Device>> ListAsync(Guid homeId, Guid? roomId, string? cursor, int limit, CancellationToken ct = default);

    /// <summary>
    /// Checks if a device with the given external ID exists.
    /// </summary>
    Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a device with the given ID exists in the specified home.
    /// </summary>
    Task<bool> ExistsByIdAndHomeAsync(Guid deviceId, Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a device by setting RemovedAt.
    /// </summary>
    Task SoftDeleteAsync(Guid deviceId, CancellationToken ct = default);
}