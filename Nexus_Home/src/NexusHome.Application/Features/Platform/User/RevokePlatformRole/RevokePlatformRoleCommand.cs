namespace NexusHome.Application.Features.Platform.User.RevokePlatformRole;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to revoke a platform role from a user.
/// </summary>
public sealed record RevokePlatformRoleCommand(
    Guid RequesterUserId,
    Guid TargetUserId,
    string RoleKey,
    Guid? ScopeId = null,
    string? ScopeType = null) : IRequest<Result<RevokePlatformRoleResponse>>;

/// <summary>
/// Response for RevokePlatformRoleCommand.
/// </summary>
public sealed record RevokePlatformRoleResponse(
    Guid UserId,
    string RoleKey,
    bool Revoked);