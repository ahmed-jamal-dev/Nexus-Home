namespace NexusHome.Application.Features.Platform.User.ListUsers;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;

/// <summary>
/// Handler for ListUsersQuery.
/// </summary>
public sealed class ListUsersHandler : IRequestHandler<ListUsersQuery, Result<ListUsersResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public ListUsersHandler(IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<ListUsersResponse>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("UserRead")
        // which validates the user:read scope in the JWT token

        // Get users with roles
        var users = await _userPlatformRoleRepository.GetUsersWithRolesAsync(
            request.Search,
            request.IsActive,
            request.Role,
            request.Cursor,
            request.Limit,
            cancellationToken);

        // Get total count
        var totalCount = await _userPlatformRoleRepository.GetUsersCountAsync(
            request.Search,
            request.IsActive,
            request.Role,
            cancellationToken);

        var userItems = users.Select(u => new UserListItem(
            u.UserId,
            u.Email,
            u.FullName,
            u.IsActive,
            u.EmailVerified,
            u.CreatedAt,
            u.UpdatedAt,
            u.RoleKeys
        )).ToList();

        string? nextCursor = null;
        if (users.Count > request.Limit)
        {
            nextCursor = users[request.Limit - 1].UserId.ToString();
        }

        return Result<ListUsersResponse>.Success(new ListUsersResponse(userItems, nextCursor, totalCount));
    }
}