namespace NexusHome.Application.Features.Platform.User.AssignPlatformRole;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to assign a platform role to a user.
/// </summary>
public sealed record AssignPlatformRoleCommand(
    Guid RequesterUserId,
    Guid TargetUserId,
    string RoleKey,
    Guid? ScopeId = null,
    string? ScopeType = null,
    DateTimeOffset? ExpiresAt = null) : IRequest<Result<AssignPlatformRoleResponse>>;

/// <summary>
/// Response for AssignPlatformRoleCommand.
/// </summary>
public sealed record AssignPlatformRoleResponse(
    Guid UserId,
    string RoleKey,
    Guid? ScopeId,
    string? ScopeType);