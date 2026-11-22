namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of user platform role repository.
/// </summary>
public sealed class UserPlatformRoleRepository : IUserPlatformRoleRepository
{
    private readonly NexusHomeDbContext _context;

    public UserPlatformRoleRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<UserPlatformRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPlatformRoles
            .Where(upr => upr.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserPlatformRole>> GetByUserIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserPlatformRole>> GetByUserIdAndRoleKeyAsync(Guid userId, string roleKey, CancellationToken cancellationToken = default)
    {
        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId && upr.PlatformRole.Key == roleKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserPlatformRole>> GetByUserIdAndScopeAsync(Guid userId, string scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId && upr.ScopeType == scopeType && upr.ScopeId == scopeId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRoleAsync(Guid userId, string roleKey, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId
                && upr.PlatformRole.Key == roleKey
                && upr.IsActive
                && (upr.ExpiresAt == null || upr.ExpiresAt > now));

        if (scopeId.HasValue)
        {
            query = query.Where(upr => upr.ScopeId == scopeId.Value);
        }

        if (!string.IsNullOrEmpty(scopeType))
        {
            query = query.Where(upr => upr.ScopeType == scopeType);
        }
        else if (!scopeId.HasValue)
        {
            // If no scope specified, match only global (null scope) roles
            query = query.Where(upr => upr.ScopeId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRoleAtLeastAsync(Guid userId, int minimumHierarchyLevel, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => upr.UserId == userId
                && upr.PlatformRole.HierarchyLevel >= minimumHierarchyLevel
                && upr.IsActive
                && (upr.ExpiresAt == null || upr.ExpiresAt > now));

        if (scopeId.HasValue)
        {
            query = query.Where(upr => upr.ScopeId == scopeId.Value);
        }

        if (!string.IsNullOrEmpty(scopeType))
        {
            query = query.Where(upr => upr.ScopeType == scopeType);
        }
        else if (!scopeId.HasValue)
        {
            query = query.Where(upr => upr.ScopeId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default)
    {
        _context.UserPlatformRoles.Add(userPlatformRole);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserPlatformRole userPlatformRole, CancellationToken cancellationToken = default)
    {
        _context.UserPlatformRoles.Update(userPlatformRole);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(Guid userId, Guid platformRoleId, Guid? scopeId = null, string? scopeType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.UserPlatformRoles
            .Where(upr => upr.UserId == userId && upr.PlatformRoleId == platformRoleId);

        if (scopeId.HasValue)
        {
            query = query.Where(upr => upr.ScopeId == scopeId.Value);
        }

        if (!string.IsNullOrEmpty(scopeType))
        {
            query = query.Where(upr => upr.ScopeType == scopeType);
        }
        else if (!scopeId.HasValue)
        {
            query = query.Where(upr => upr.ScopeId == null);
        }

        var assignment = await query.FirstOrDefaultAsync(cancellationToken);
        if (assignment == null)
        {
            return false;
        }

        assignment.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<UserPlatformRole>> GetByRoleKeyAsync(string roleKey, CancellationToken cancellationToken = default)
    {
        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Include(upr => upr.User)
            .Where(upr => upr.PlatformRole.Key == roleKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasPlatformAdminOrHigherAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .AnyAsync(upr => upr.UserId == userId
                && upr.IsActive
                && (upr.ExpiresAt == null || upr.ExpiresAt > now)
                && (upr.PlatformRole.Key == "SuperAdmin" || upr.PlatformRole.Key == "PlatformAdmin"), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetRoleCountsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.PlatformRoles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, int>();
        foreach (var role in roles)
        {
            var count = await _context.UserPlatformRoles
                .CountAsync(upr => upr.PlatformRoleId == role.Id && upr.IsActive, cancellationToken);
            result[role.Key] = count;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<(Guid UserId, string Email, string FullName, bool IsActive, bool EmailVerified, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<string> RoleKeys)>> GetUsersWithRolesAsync(
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
    public async Task<List<UserPlatformRole>> GetByUserIdsWithRolesAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return new List<UserPlatformRole>();
        }

        return await _context.UserPlatformRoles
            .Include(upr => upr.PlatformRole)
            .Where(upr => userIds.Contains(upr.UserId) && upr.IsActive)
            .ToListAsync(cancellationToken);
    }
}