namespace NexusHome.Application.Features.Home.Permission.RevokePermission;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to revoke a permission from a member.
/// </summary>
public sealed record RevokePermissionCommand(
    Guid UserId,
    Guid HomeId,
    Guid PermissionId) : IRequest<Result<RevokePermissionResponse>>;