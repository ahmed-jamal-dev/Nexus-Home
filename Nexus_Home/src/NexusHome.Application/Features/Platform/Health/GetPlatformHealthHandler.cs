namespace NexusHome.Application.Features.Platform.Health;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;

/// <summary>
/// Handler for GetPlatformHealthQuery.
/// </summary>
public sealed class GetPlatformHealthHandler : IRequestHandler<GetPlatformHealthQuery, Result<GetPlatformHealthResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IPlatformRoleRepository _platformRoleRepository;

    public GetPlatformHealthHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IUserRepository userRepository,
        IHomeRepository homeRepository,
        IPlatformRoleRepository platformRoleRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _userRepository = userRepository;
        _homeRepository = homeRepository;
        _platformRoleRepository = platformRoleRepository;
    }

    public async Task<Result<GetPlatformHealthResponse>> Handle(GetPlatformHealthQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("PlatformHealth")
        // which validates the platform:health scope in the JWT token

        // Get counts
        var totalUsers = await _userRepository.GetTotalCountAsync(cancellationToken);
        var activeUsers = await _userRepository.GetActiveCountAsync(cancellationToken);
        var totalHomes = await _homeRepository.GetTotalCountAsync(cancellationToken);
        var activeHomes = await _homeRepository.GetActiveCountAsync(cancellationToken);
        var totalDevices = await _homeRepository.GetTotalDeviceCountAsync(cancellationToken);
        var onlineDevices = await _homeRepository.GetOnlineDeviceCountAsync(cancellationToken);

        // Get platform role counts
        var platformRolesWithCounts = await _platformRoleRepository.GetRolesWithCountsAsync(cancellationToken);

        var roleCounts = platformRolesWithCounts.Select(r => new PlatformRoleSummary(r.Role.Key, r.Role.Name, r.Count)).ToList();

        return Result<GetPlatformHealthResponse>.Success(new GetPlatformHealthResponse(
            totalUsers,
            activeUsers,
            totalHomes,
            activeHomes,
            totalDevices,
            onlineDevices,
            platformRolesWithCounts.Count,
            roleCounts));
    }
}