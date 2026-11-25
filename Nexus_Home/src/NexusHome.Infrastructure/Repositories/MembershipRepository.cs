namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Data;
using Npgsql;

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

    /// <inheritdoc />
    public async Task<Membership?> GetByUserAndHomeAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireLastOwnerLockAsync(Guid homeId, CancellationToken ct = default)
    {
        // Derive a stable int64 key from the Guid (first 8 bytes) for PostgreSQL advisory lock
        var keyBytes = homeId.ToByteArray();
        var lockKey = BitConverter.ToInt64(keyBytes, 0);

        // pg_try_advisory_xact_lock returns true if lock acquired, false if already held
        // Transaction-scoped: auto-released on commit or rollback
        var sql = "SELECT pg_try_advisory_xact_lock(@lockKey)";
        var result = await _context.Database.SqlQueryRaw<bool>(sql, new NpgsqlParameter("lockKey", lockKey)).FirstAsync(ct);
        return result;
    }

    /// <inheritdoc />
    public async Task ReleaseLastOwnerLockAsync(Guid homeId, CancellationToken ct = default)
    {
        // No-op for transaction-scoped locks (pg_advisory_xact_unlock is automatic at transaction end).
        // Kept for interface symmetry and potential future session-scoped locks.
        await Task.CompletedTask;
    }
}