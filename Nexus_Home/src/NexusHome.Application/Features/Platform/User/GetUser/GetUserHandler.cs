namespace NexusHome.Application.Features.Platform.User.GetUser;

using MediatR;
using NexusHome.Application.Common;
using static NexusHome.Application.Common.Errors;
using NexusHome.Application.Interfaces;

/// <summary>
/// Handler for GetUserQuery.
/// </summary>
public sealed class GetUserHandler : IRequestHandler<GetUserQuery, Result<GetUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public GetUserHandler(
        IUserRepository userRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _userRepository = userRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<GetUserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("UserRead")
        // which validates the user:read scope in the JWT token

        // Get user detail with roles and memberships
        var detail = await _userRepository.GetUserDetailWithRolesAndMembershipsAsync(request.TargetUserId, cancellationToken);

        if (detail == null)
        {
            return Result<GetUserResponse>.Failure(Platform.UserNotFound);
        }

        var (user, platformRoles, memberships) = detail.Value;

        var roleDetails = platformRoles.Select(r => new UserPlatformRoleDetail(
            r.Role.PlatformRole.Id,
            r.Role.PlatformRole.Key,
            r.Role.PlatformRole.Name,
            r.Role.PlatformRole.HierarchyLevel,
            r.Role.ScopeId,
            r.Role.ScopeType,
            r.Role.AssignedAt,
            r.Role.ExpiresAt,
            r.Role.AssignedBy,
            r.AssignedByEmail
        )).ToList();

        var membershipSummaries = memberships.Select(m => new HomeMembershipSummary(
            m.Membership.HomeId,
            m.Membership.Home?.Name ?? "Unknown",
            m.Membership.Role.ToString(),
            m.Membership.CreatedAt
        )).ToList();

        var userDetail = new UserDetail(
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            user.EmailVerified,
            user.CreatedAt,
            user.UpdatedAt,
            roleDetails,
            membershipSummaries
        );

        return Result<GetUserResponse>.Success(new GetUserResponse(userDetail));
    }
}