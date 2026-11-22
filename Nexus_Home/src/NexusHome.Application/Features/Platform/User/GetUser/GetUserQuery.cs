namespace NexusHome.Application.Features.Platform.User.GetUser;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public sealed record GetUserQuery(
    Guid RequesterUserId,
    Guid TargetUserId) : IRequest<Result<GetUserResponse>>;

/// <summary>
/// Response for GetUserQuery.
/// </summary>
public sealed record GetUserResponse(
    UserDetail User);

/// <summary>
/// Detailed user information for platform administration.
/// </summary>
public sealed record UserDetail(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<UserPlatformRoleDetail> PlatformRoles,
    List<HomeMembershipSummary> HomeMemberships);

/// <summary>
/// Platform role detail for user.
/// </summary>
public sealed record UserPlatformRoleDetail(
    Guid RoleId,
    string RoleKey,
    string RoleName,
    int HierarchyLevel,
    Guid? ScopeId,
    string? ScopeType,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ExpiresAt,
    Guid AssignedBy,
    string AssignedByEmail);

/// <summary>
/// Home membership summary for user.
/// </summary>
public sealed record HomeMembershipSummary(
    Guid HomeId,
    string HomeName,
    string Role,
    DateTimeOffset JoinedAt);