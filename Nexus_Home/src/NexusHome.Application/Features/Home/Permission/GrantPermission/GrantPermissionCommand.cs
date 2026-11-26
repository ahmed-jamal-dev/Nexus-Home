namespace NexusHome.Application.Features.Home.Permission.GrantPermission;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Domain.Enums;

/// <summary>
/// Command to grant a permission to a member.
/// </summary>
public sealed record GrantPermissionCommand(
    Guid UserId,
    Guid HomeId,
    Guid GranteeMembershipId,
    string PermissionType,
    string TargetType,
    Guid TargetId,
    string Effect = "Grant") : IRequest<Result<GrantPermissionResponse>>;