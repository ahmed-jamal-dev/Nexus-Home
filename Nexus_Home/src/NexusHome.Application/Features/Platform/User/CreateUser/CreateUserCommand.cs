namespace NexusHome.Application.Features.Platform.User.CreateUser;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to create a new user (platform admin only).
/// </summary>
public sealed record CreateUserCommand(
    Guid RequesterUserId,
    string Email,
    string FullName,
    string Password,
    List<CreateUserRoleAssignment> RoleAssignments) : IRequest<Result<CreateUserResponse>>;

/// <summary>
/// Role assignment for new user.
/// </summary>
public sealed record CreateUserRoleAssignment(
    string RoleKey,
    Guid? ScopeId = null,
    string? ScopeType = null);

/// <summary>
/// Response for CreateUserCommand.
/// </summary>
public sealed record CreateUserResponse(
    Guid UserId,
    string Email,
    string FullName);