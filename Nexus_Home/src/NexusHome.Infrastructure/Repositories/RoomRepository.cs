namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of room repository.
/// </summary>
public sealed class RoomRepository : IRoomRepository
{
    private readonly NexusHomeDbContext _context;

    public RoomRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Room?> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Room?> GetByIdWithHomeAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .Include(r => r.Home)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Room>> ListAsync(Guid homeId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Rooms
            .Where(r => r.HomeId == homeId);

        if (!string.IsNullOrEmpty(cursor))
        {
            if (Guid.TryParse(cursor, out var cursorId))
            {
                var cursorRoom = await _context.Rooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == cursorId, cancellationToken);

                if (cursorRoom != null)
                {
                    query = query.Where(r => r.CreatedAt > cursorRoom.CreatedAt ||
                        (r.CreatedAt == cursorRoom.CreatedAt && r.Id.CompareTo(cursorRoom.Id) > 0));
                }
            }
        }

        return await query
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(Guid homeId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .AnyAsync(r => r.HomeId == homeId && r.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var room = await _context.Rooms
            .Include(r => r.Devices)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        if (room != null)
        {
            // Clear RoomId for all devices in this room before soft-deleting
            foreach (var device in room.Devices)
            {
                device.AssignToRoom(null);
            }

            room.Delete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}