namespace NexusHome.Application.Features.Home.Permission.ListPermissions;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list permissions for a home or member.
/// </summary>
public sealed record ListPermissionsQuery(
    Guid UserId,
    Guid HomeId,
    Guid? GranteeMembershipId = null,
    string? TargetType = null,
    Guid? TargetId = null,
    string? Cursor = null,
    int Limit = 50) : IRequest<Result<ListPermissionsResponse>>;