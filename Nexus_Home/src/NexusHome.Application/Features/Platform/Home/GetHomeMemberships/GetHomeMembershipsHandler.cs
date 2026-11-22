namespace NexusHome.Application.Features.Platform.Home.GetHomeMemberships;

using MediatR;
using NexusHome.Application.Common;
using static NexusHome.Application.Common.Errors;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;

/// <summary>
/// Handler for GetHomeMembershipsQuery.
/// </summary>
public sealed class GetHomeMembershipsHandler : IRequestHandler<GetHomeMembershipsQuery, Result<GetHomeMembershipsResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IHomeRepository _homeRepository;

    public GetHomeMembershipsHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IHomeRepository homeRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _homeRepository = homeRepository;
    }

    public async Task<Result<GetHomeMembershipsResponse>> Handle(GetHomeMembershipsQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("HomeManage")
        // which validates the home:manage scope in the JWT token

        // Get home
        var home = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);

        if (home == null)
        {
            return Result<GetHomeMembershipsResponse>.Failure(Platform.HomeNotFound);
        }

        // Get memberships
        var memberships = await _homeRepository.GetHomeMembershipsAsync(request.HomeId, cancellationToken);

        var userIds = memberships.Select(m => m.Membership.UserId).Distinct().ToList();

        // Get platform roles for each user
        var userPlatformRoles = await _userPlatformRoleRepository.GetByUserIdsWithRolesAsync(userIds, cancellationToken);

        var userPlatformRoleMap = userPlatformRoles
            .GroupBy(upr => upr.UserId)
            .ToDictionary(g => g.Key, g => g.Select(upr => upr.PlatformRole.Key).Distinct().ToList());

        var membershipDetails = memberships.Select(m => new HomeMembershipDetail(
            m.Membership.UserId,
            m.UserEmail,
            m.Membership.User?.FullName ?? "Unknown",
            m.Membership.Role.ToString(),
            m.Membership.CreatedAt,
            userPlatformRoleMap.GetValueOrDefault(m.Membership.UserId, new List<string>())
        )).ToList();

        return Result<GetHomeMembershipsResponse>.Success(new GetHomeMembershipsResponse(membershipDetails));
    }
}