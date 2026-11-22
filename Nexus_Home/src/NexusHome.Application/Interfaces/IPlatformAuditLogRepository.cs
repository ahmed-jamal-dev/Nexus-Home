namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository for platform audit log operations.
/// </summary>
public interface IPlatformAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    Task AddAsync(PlatformAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists audit logs with filtering and pagination, including actor emails.
    /// </summary>
    Task<List<(PlatformAuditLog Log, string ActorEmail)>> ListWithActorEmailAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? cursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists audit logs with filtering and pagination.
    /// </summary>
    Task<List<PlatformAuditLog>> ListAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? cursor = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of audit logs matching the filter.
    /// </summary>
    Task<int> CountAsync(
        Guid? actorUserId = null,
        string? action = null,
        string? targetType = null,
        Guid? targetId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);
}