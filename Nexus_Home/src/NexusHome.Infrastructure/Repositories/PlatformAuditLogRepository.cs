namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of platform audit log repository.
/// </summary>
public sealed class PlatformAuditLogRepository : IPlatformAuditLogRepository
{
    private readonly NexusHomeDbContext _context;

    public PlatformAuditLogRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(PlatformAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _context.PlatformAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<(PlatformAuditLog Log, string ActorEmail)>> ListWithActorEmailAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? cursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PlatformAuditLogs
            .Include(log => log.ActorUser)
            .AsQueryable();

        if (actorUserId.HasValue)
        {
            query = query.Where(log => log.ActorUserId == actorUserId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(log => log.Action == action);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            query = query.Where(log => log.TargetType == targetType);
        }

        if (targetId.HasValue)
        {
            query = query.Where(log => log.TargetId == targetId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            if (Guid.TryParse(cursor, out var cursorId))
            {
                var cursorLog = await _context.PlatformAuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(log => log.Id == cursorId, cancellationToken);

                if (cursorLog != null)
                {
                    query = query.Where(log => log.CreatedAt > cursorLog.CreatedAt ||
                        (log.CreatedAt == cursorLog.CreatedAt && log.Id.CompareTo(cursorLog.Id) > 0));
                }
            }
        }

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return logs.Select(log => (log, log.ActorUser?.Email ?? "Unknown")).ToList();
    }

    /// <inheritdoc />
    public async Task<List<PlatformAuditLog>> ListAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? cursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PlatformAuditLogs
            .Include(log => log.ActorUser)
            .AsQueryable();

        if (actorUserId.HasValue)
        {
            query = query.Where(log => log.ActorUserId == actorUserId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(log => log.Action == action);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            query = query.Where(log => log.TargetType == targetType);
        }

        if (targetId.HasValue)
        {
            query = query.Where(log => log.TargetId == targetId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            if (Guid.TryParse(cursor, out var cursorId))
            {
                var cursorLog = await _context.PlatformAuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(log => log.Id == cursorId, cancellationToken);

                if (cursorLog != null)
                {
                    query = query.Where(log => log.CreatedAt > cursorLog.CreatedAt ||
                        (log.CreatedAt == cursorLog.CreatedAt && log.Id.CompareTo(cursorLog.Id) > 0));
                }
            }
        }

        return await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PlatformAuditLogs.AsQueryable();

        if (actorUserId.HasValue)
        {
            query = query.Where(log => log.ActorUserId == actorUserId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(log => log.Action == action);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            query = query.Where(log => log.TargetType == targetType);
        }

        if (targetId.HasValue)
        {
            query = query.Where(log => log.TargetId == targetId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= to.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}