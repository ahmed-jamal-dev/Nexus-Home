namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of membership repository.
/// </summary>
public sealed class MembershipRepository : IMembershipRepository
{
    private readonly NexusHomeDbContext _context;

    public MembershipRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _context.Memberships.Update(membership);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Membership?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Membership?> GetActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .FirstOrDefaultAsync(m => m.HomeId == homeId && m.IsActive && m.Role == Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveOwnerAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive && m.Role == Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Membership>> ListAsync(Guid homeId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Memberships
            .Include(m => m.User)
            .Where(m => m.HomeId == homeId && m.IsActive);

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
                    query = query.Where(m => m.CreatedAt > cursorCreatedAt ||
                        (m.CreatedAt == cursorCreatedAt && m.Id.CompareTo(cursorId) > 0));
                }
            }
            catch
            {
                // Invalid cursor format - ignore and return from beginning
            }
        }

        return await query
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}