namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of home repository.
/// </summary>
public sealed class HomeRepository : IHomeRepository
{
    private readonly NexusHomeDbContext _context;

    public HomeRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Home?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Home?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .Include(h => h.Memberships.Where(m => m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner))
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(Home Home, string OwnerEmail, int MemberCount, int DeviceCount, Guid? ProvisionedByTechnicianId)?> GetHomeWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var home = await _context.Homes
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);

        if (home == null)
        {
            return null;
        }

        var ownerMembership = await _context.Memberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.HomeId == id && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner, cancellationToken);

        var memberCount = await _context.Memberships
            .CountAsync(m => m.HomeId == id, cancellationToken);

        var deviceCount = await _context.Devices
            .CountAsync(d => d.HomeId == id && d.RemovedAt == null, cancellationToken);

        return (home, ownerMembership?.User?.Email ?? "Unknown", memberCount, deviceCount, home.ProvisionedByTechnicianId);
    }

    /// <inheritdoc />
    public async Task<List<(Home Home, string OwnerEmail, int MemberCount, int DeviceCount, Guid? ProvisionedByTechnicianId)>> GetHomesAsync(
        string? search = null,
        bool? isDeleted = null,
        List<Guid>? technicianScope = null,
        bool isPlatformAdminOrHigher = false,
        string? cursor = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Homes.AsQueryable();

        // Apply Technician scope filter
        if (technicianScope != null && technicianScope.Count > 0 && !isPlatformAdminOrHigher)
        {
            query = query.Where(h => technicianScope.Contains(h.Id));
        }

        // Apply isDeleted filter
        if (isDeleted.HasValue)
        {
            query = query.Where(h => (h.DeletedAt != null) == isDeleted.Value);
        }
        else
        {
            // Default to non-deleted homes
            query = query.Where(h => h.DeletedAt == null);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                (h.City != null && h.City.ToLower().Contains(searchLower)) ||
                (h.StateProvince != null && h.StateProvince.ToLower().Contains(searchLower)));
        }

        // Apply cursor-based pagination
        if (!string.IsNullOrEmpty(cursor))
        {
            if (Guid.TryParse(cursor, out var cursorId))
            {
                var cursorHome = await _context.Homes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == cursorId, cancellationToken);

                if (cursorHome != null)
                {
                    query = query.Where(h => h.CreatedAt > cursorHome.CreatedAt ||
                        (h.CreatedAt == cursorHome.CreatedAt && h.Id.CompareTo(cursorHome.Id) > 0));
                }
            }
        }

        var homes = await query
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var homeIds = homes.Select(h => h.Id).ToList();

        // Get owner info
        var ownerMemberships = await _context.Memberships
            .Include(m => m.User)
            .Where(m => homeIds.Contains(m.HomeId) && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner)
            .ToListAsync(cancellationToken);

        // Get member counts
        var memberCounts = await _context.Memberships
            .Where(m => homeIds.Contains(m.HomeId))
            .GroupBy(m => m.HomeId)
            .Select(g => new { HomeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HomeId, x => x.Count, cancellationToken);

        // Get device counts
        var deviceCounts = await _context.Devices
            .Where(d => homeIds.Contains(d.HomeId) && d.RemovedAt == null)
            .GroupBy(d => d.HomeId)
            .Select(g => new { HomeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HomeId, x => x.Count, cancellationToken);

        var result = homes.Take(limit).Select(h => (
            h,
            ownerMemberships.FirstOrDefault(m => m.HomeId == h.Id)?.User?.Email ?? "Unknown",
            memberCounts.GetValueOrDefault(h.Id, 0),
            deviceCounts.GetValueOrDefault(h.Id, 0),
            h.ProvisionedByTechnicianId
        )).ToList();

        return result;
    }

    /// <inheritdoc />
    public async Task<int> GetHomesCountAsync(
        string? search = null,
        bool? isDeleted = null,
        List<Guid>? technicianScope = null,
        bool isPlatformAdminOrHigher = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Homes.AsQueryable();

        // Apply Technician scope filter
        if (technicianScope != null && technicianScope.Count > 0 && !isPlatformAdminOrHigher)
        {
            query = query.Where(h => technicianScope.Contains(h.Id));
        }

        // Apply isDeleted filter
        if (isDeleted.HasValue)
        {
            query = query.Where(h => (h.DeletedAt != null) == isDeleted.Value);
        }
        else
        {
            // Default to non-deleted homes
            query = query.Where(h => h.DeletedAt == null);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(h => h.Name.ToLower().Contains(searchLower) ||
                (h.City != null && h.City.ToLower().Contains(searchLower)) ||
                (h.StateProvince != null && h.StateProvince.ToLower().Contains(searchLower)));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<(Membership Membership, string UserEmail)>> GetHomeMembershipsAsync(
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _context.Memberships
            .Include(m => m.User)
            .Where(m => m.HomeId == homeId)
            .ToListAsync(cancellationToken);

        return memberships.Select(m => (m, m.User?.Email ?? "Unknown")).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Device>> GetHomeDevicesAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Room)
            .Where(d => d.HomeId == homeId && d.RemovedAt == null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Room>> GetHomeRoomsAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .Where(r => r.HomeId == homeId && r.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Home home, CancellationToken cancellationToken = default)
    {
        _context.Homes.Add(home);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Home home, CancellationToken cancellationToken = default)
    {
        _context.Homes.Update(home);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .AnyAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.HomeId == homeId && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UserOwnsHomeAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsProvisionedByTechnicianAsync(Guid homeId, Guid technicianUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .AnyAsync(h => h.Id == homeId && h.ProvisionedByTechnicianId == technicianUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Homes.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Homes.CountAsync(h => h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalDeviceCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices.CountAsync(d => d.RemovedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetOnlineDeviceCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices.CountAsync(d => d.RemovedAt == null && d.ConnectionStatus == ConnectionStatus.Online, cancellationToken);
    }
}