namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of device repository.
/// </summary>
public sealed class DeviceRepository : IDeviceRepository
{
    private readonly NexusHomeDbContext _context;

    public DeviceRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Device?> GetByIdWithHomeAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Home)
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Device?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Device>> ListAsync(Guid homeId, Guid? roomId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices
            .Where(d => d.HomeId == homeId);

        if (roomId.HasValue)
        {
            query = query.Where(d => d.RoomId == roomId.Value);
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                var cursorBytes = Convert.FromBase64String(cursor);
                var cursorString = System.Text.Encoding.UTF8.GetString(cursorBytes);
                var cursorParts = cursorString.Split('|');
                if (cursorParts.Length == 2 &&
                    DateTimeOffset.TryParse(cursorParts[0], out var cursorCreatedAt) &&
                    Guid.TryParse(cursorParts[1], out var cursorId))
                {
                    query = query.Where(d => d.CreatedAt > cursorCreatedAt ||
                        (d.CreatedAt == cursorCreatedAt && d.Id.CompareTo(cursorId) > 0));
                }
            }
            catch
            {
                // Invalid cursor format - ignore and return from beginning
            }
        }

        return await query
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .AnyAsync(d => d.ExternalId == externalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdAndHomeAsync(Guid deviceId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .AnyAsync(d => d.Id == deviceId && d.HomeId == homeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device != null)
        {
            device.Remove();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}