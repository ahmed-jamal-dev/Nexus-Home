namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Converters;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of permission repository.
/// </summary>
public sealed class PermissionRepository : IPermissionRepository
{
    private readonly NexusHomeDbContext _context;
    private readonly ValueConverter<PermissionType, string> _permissionTypeConverter = new PermissionTypeConverter();
    private readonly ValueConverter<TargetType, string> _targetTypeConverter = new TargetTypeConverter();
    private readonly ValueConverter<Effect, string> _effectConverter = new EffectConverter();

    public PermissionRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    private string ConvertPermissionType(PermissionType v) => (string)_permissionTypeConverter.ConvertToProvider(v);
    private string ConvertTargetType(TargetType v) => (string)_targetTypeConverter.ConvertToProvider(v);
    private string ConvertEffect(Effect v) => (string)_effectConverter.ConvertToProvider(v);

    /// <inheritdoc />
    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        // Check for explicit deny first (deny overrides grant)
        var hasDeny = await _context.Permissions
            .AnyAsync(p =>
                p.GranteeMembershipId == granteeMembershipId &&
                p.PermissionType == permissionType &&
                p.TargetType == targetType &&
                p.TargetId == targetId &&
                p.Effect == Effect.Deny,
                cancellationToken);

        if (hasDeny)
        {
            return false;
        }

        // Check for grant
        var hasGrant = await _context.Permissions
            .AnyAsync(p =>
                p.GranteeMembershipId == granteeMembershipId &&
                p.PermissionType == permissionType &&
                p.TargetType == targetType &&
                p.TargetId == targetId &&
                p.Effect == Effect.Grant,
                cancellationToken);

        return hasGrant;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionEffectiveAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        // Same logic as HasPermissionAsync - deny overrides grant
        return await HasPermissionAsync(granteeMembershipId, permissionType, targetType, targetId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Permission>> ListByGranteeAsync(Guid granteeMembershipId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.GranteeMembershipId == granteeMembershipId)
            .OrderBy(p => p.PermissionType)
            .ThenBy(p => p.TargetType)
            .ThenBy(p => p.TargetId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Permission>> ListByHomeAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.HomeId == homeId)
            .OrderBy(p => p.GranteeMembershipId)
            .ThenBy(p => p.PermissionType)
            .ThenBy(p => p.TargetType)
            .ThenBy(p => p.TargetId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Permission>> ListByTargetAsync(TargetType targetType, Guid targetId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.TargetType == targetType && p.TargetId == targetId)
            .OrderBy(p => p.GranteeMembershipId)
            .ThenBy(p => p.PermissionType)
            .ThenBy(p => p.Effect)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Permission>> ListByHomeWithDetailsAsync(
        Guid homeId,
        Guid? granteeMembershipId,
        TargetType? targetType,
        Guid? targetId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Permissions
            .Include(p => p.GranteeMembership)
                .ThenInclude(m => m.User)
            .Include(p => p.GrantorMembership)
                .ThenInclude(m => m.User)
            .Where(p => p.HomeId == homeId);

        if (granteeMembershipId.HasValue)
        {
            query = query.Where(p => p.GranteeMembershipId == granteeMembershipId.Value);
        }

        if (targetType.HasValue)
        {
            query = query.Where(p => p.TargetType == targetType.Value);
        }

        if (targetId.HasValue)
        {
            query = query.Where(p => p.TargetId == targetId.Value);
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
                    query = query.Where(p => p.CreatedAt > cursorCreatedAt ||
                        (p.CreatedAt == cursorCreatedAt && p.Id.CompareTo(cursorId) > 0));
                }
            }
            catch
            {
                // Invalid cursor format - ignore
            }
        }

        return await query
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(limit + 1) // Take one extra for cursor
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Guid granteeMembershipId,
        PermissionType permissionType,
        TargetType targetType,
        Guid targetId,
        Effect effect,
        CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AnyAsync(p =>
                p.GranteeMembershipId == granteeMembershipId &&
                p.PermissionType == permissionType &&
                p.TargetType == targetType &&
                p.TargetId == targetId &&
                p.Effect == effect,
                cancellationToken);
    }
}