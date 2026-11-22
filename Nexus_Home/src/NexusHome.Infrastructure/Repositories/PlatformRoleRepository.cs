namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of platform role repository.
/// </summary>
public sealed class PlatformRoleRepository : IPlatformRoleRepository
{
    private readonly NexusHomeDbContext _context;

    public PlatformRoleRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PlatformRole?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Key == key && r.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlatformRole?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<PlatformRole>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PlatformRoles
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.HierarchyLevel)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PlatformRole role, CancellationToken cancellationToken = default)
    {
        _context.PlatformRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PlatformRole role, CancellationToken cancellationToken = default)
    {
        _context.PlatformRoles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null || role.IsSystemRole)
        {
            return false;
        }

        role.Delete();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<(PlatformRole Role, int Count)>> GetRolesWithCountsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.PlatformRoles
            .OrderBy(r => r.HierarchyLevel)
            .ToListAsync(cancellationToken);

        var result = new List<(PlatformRole Role, int Count)>();
        foreach (var role in roles)
        {
            var count = await _context.UserPlatformRoles
                .CountAsync(upr => upr.PlatformRoleId == role.Id && upr.IsActive, cancellationToken);
            result.Add((role, count));
        }

        return result;
    }
}