namespace NexusHome.Application.Features.Platform.AuditLog;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list platform audit logs with filtering and pagination.
/// </summary>
public sealed record ListPlatformAuditLogsQuery(
    Guid RequesterUserId,
    Guid? ActorUserId = null,
    string? Action = null,
    string? TargetType = null,
    Guid? TargetId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Cursor = null,
    int Limit = 50) : IRequest<Result<ListPlatformAuditLogsResponse>>;

/// <summary>
/// Response for ListPlatformAuditLogsQuery.
/// </summary>
public sealed record ListPlatformAuditLogsResponse(
    List<PlatformAuditLogItem> Logs,
    string? NextCursor,
    int TotalCount);

/// <summary>
/// Platform audit log item.
/// </summary>
public sealed record PlatformAuditLogItem(
    Guid Id,
    Guid ActorUserId,
    string ActorEmail,
    string Action,
    string TargetType,
    Guid TargetId,
    string? Details,
    string? IpAddress,
    string? UserAgent,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);