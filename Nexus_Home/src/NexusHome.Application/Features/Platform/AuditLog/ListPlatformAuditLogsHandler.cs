namespace NexusHome.Application.Features.Platform.AuditLog;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;

/// <summary>
/// Handler for ListPlatformAuditLogsQuery.
/// </summary>
public sealed class ListPlatformAuditLogsHandler : IRequestHandler<ListPlatformAuditLogsQuery, Result<ListPlatformAuditLogsResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public ListPlatformAuditLogsHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<ListPlatformAuditLogsResponse>> Handle(ListPlatformAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("PlatformAudit")
        // which validates the platform:audit scope in the JWT token

        // Get logs with actor emails
        var logs = await _auditLogRepository.ListWithActorEmailAsync(
            request.ActorUserId,
            request.Action,
            request.TargetType,
            request.TargetId,
            request.From,
            request.To,
            request.Cursor,
            request.Limit + 1, // Take one extra to detect next page
            cancellationToken);

        var totalCount = await _auditLogRepository.CountAsync(
            request.ActorUserId,
            request.Action,
            request.TargetType,
            request.TargetId,
            request.From,
            request.To,
            cancellationToken);

        var logItems = logs.Take(request.Limit).Select(l => new PlatformAuditLogItem(
            l.Log.Id,
            l.Log.ActorUserId,
            l.ActorEmail,
            l.Log.Action,
            l.Log.TargetType,
            l.Log.TargetId,
            l.Log.Details,
            l.Log.IpAddress,
            l.Log.UserAgent,
            l.Log.Success,
            l.Log.ErrorMessage,
            l.Log.CreatedAt
        )).ToList();

        string? nextCursor = null;
        if (logs.Count > request.Limit)
        {
            nextCursor = logs[request.Limit - 1].Log.Id.ToString();
        }

        return Result<ListPlatformAuditLogsResponse>.Success(new ListPlatformAuditLogsResponse(logItems, nextCursor, totalCount));
    }
}