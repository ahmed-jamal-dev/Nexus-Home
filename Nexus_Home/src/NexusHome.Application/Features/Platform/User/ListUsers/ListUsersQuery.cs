namespace NexusHome.Application.Features.Platform.User.ListUsers;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list users with pagination and filtering.
/// </summary>
public sealed record ListUsersQuery(
    Guid RequesterUserId,
    string? Search = null,
    bool? IsActive = null,
    string? Role = null,
    string? Cursor = null,
    int Limit = 20) : IRequest<Result<ListUsersResponse>>;

/// <summary>
/// Response for ListUsersQuery.
/// </summary>
public sealed record ListUsersResponse(
    List<UserListItem> Users,
    string? NextCursor,
    int TotalCount);

/// <summary>
/// User list item for platform administration.
/// </summary>
public sealed record UserListItem(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    bool EmailVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    List<string> PlatformRoles);