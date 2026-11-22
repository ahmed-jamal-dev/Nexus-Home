namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of user repository.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly NexusHomeDbContext _context;

    public UserRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<(Guid UserId, string Email, string FullName, bool IsActive, bool EmailVerified, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<string> RoleKeys)>> GetUsersWithPlatformRolesAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        string? cursor = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(searchLower) || u.FullName.ToLower().Contains(searchLower));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => _context.UserPlatformRoles
                .Any(upr => upr.UserId == u.Id && upr.PlatformRole.Key == role && upr.IsActive));
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            if (Guid.TryParse(cursor, out var cursorId))
            {
                var cursorUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == cursorId, cancellationToken);

                if (cursorUser != null)
                {
                    query = query.Where(u => u.CreatedAt > cursorUser.CreatedAt ||
                        (u.CreatedAt == cursorUser.CreatedAt && u.Id.CompareTo(cursorUser.Id) > 0));
                }
            }
        }

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => userIds.Contains(upr.UserId) && upr.IsActive)
            .ToListAsync(cancellationToken);

        var userRoleMap = userRoles
            .GroupBy(upr => upr.UserId)
            .ToDictionary(g => g.Key, g => g.Select(upr => upr.PlatformRole.Key).Distinct().ToList());

        return users.Take(limit).Select(u => (
            u.Id,
            u.Email,
            u.FullName,
            u.IsActive,
            u.EmailVerified,
            u.CreatedAt,
            u.UpdatedAt,
            userRoleMap.GetValueOrDefault(u.Id, new List<string>())
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUsersCountAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(searchLower) || u.FullName.ToLower().Contains(searchLower));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => _context.UserPlatformRoles
                .Any(upr => upr.UserId == u.Id && upr.PlatformRole.Key == role && upr.IsActive));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(User User, List<(UserPlatformRole Role, string AssignedByEmail)> PlatformRoles, List<(Membership Membership, string UserEmail)> Memberships)?> GetUserDetailWithRolesAndMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var platformRoles = await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId && upr.IsActive)
            .ToListAsync(cancellationToken);

        var assignedByUserIds = platformRoles.Select(r => r.AssignedBy).Distinct().ToList();
        var assignedByUsers = await _context.Users
            .AsNoTracking()
            .Where(u => assignedByUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var roleDetails = platformRoles.Select(r => (
            r,
            assignedByUsers.GetValueOrDefault(r.AssignedBy, "Unknown")
        )).ToList();

        var memberships = await _context.Memberships
            .Include(m => m.Home)
            .Where(m => m.UserId == userId && m.Home != null && m.Home.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var membershipDetails = memberships.Select(m => (
            m,
            m.Home != null ? m.Home.Name : "Unknown"
        )).ToList();

        // Convert to the expected format
        var membershipsWithEmail = memberships.Select(m => (
            m,
            m.User?.Email ?? "Unknown"
        )).ToList();

        return (user, roleDetails, membershipsWithEmail);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .CountAsync(u => u.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .CountAsync(u => u.DeletedAt == null && u.IsActive, cancellationToken);
    }
}