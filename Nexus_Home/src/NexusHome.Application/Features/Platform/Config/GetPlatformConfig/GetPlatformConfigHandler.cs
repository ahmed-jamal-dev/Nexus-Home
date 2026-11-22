namespace NexusHome.Application.Features.Platform.Config.GetPlatformConfig;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;

/// <summary>
/// Handler for GetPlatformConfigQuery.
/// </summary>
public sealed class GetPlatformConfigHandler : IRequestHandler<GetPlatformConfigQuery, Result<GetPlatformConfigResponse>>
{
    private readonly IPlatformRoleRepository _platformRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHomeRepository _homeRepository;

    public GetPlatformConfigHandler(
        IPlatformRoleRepository platformRoleRepository,
        IUserRepository userRepository,
        IHomeRepository homeRepository)
    {
        _platformRoleRepository = platformRoleRepository;
        _userRepository = userRepository;
        _homeRepository = homeRepository;
    }

    public async Task<Result<GetPlatformConfigResponse>> Handle(GetPlatformConfigQuery request, CancellationToken cancellationToken)
    {
        // Authorization is handled at the endpoint level via RequireAuthorization("PlatformConfig")
        // which validates the platform:config scope in the JWT token (SuperAdmin only)

        // Get platform roles with counts
        var platformRolesWithCounts = await _platformRoleRepository.GetRolesWithCountsAsync(cancellationToken);

        var roleConfigs = platformRolesWithCounts.Select(r => new PlatformRoleConfig(
            r.Role.Key,
            r.Role.Name,
            r.Role.HierarchyLevel,
            r.Role.IsSystemRole,
            r.Count)).ToList();

        // Get user counts
        var totalUsers = await _userRepository.GetTotalCountAsync(cancellationToken);
        var activeUsers = await _userRepository.GetActiveCountAsync(cancellationToken);

        // Get home counts
        var totalHomes = await _homeRepository.GetTotalCountAsync(cancellationToken);
        var activeHomes = await _homeRepository.GetActiveCountAsync(cancellationToken);

        var settings = new Dictionary<string, string>
        {
            ["totalUsers"] = totalUsers.ToString(),
            ["activeUsers"] = activeUsers.ToString(),
            ["totalHomes"] = totalHomes.ToString(),
            ["activeHomes"] = activeHomes.ToString(),
            ["platformRolesCount"] = roleConfigs.Count.ToString()
        };

        var response = new GetPlatformConfigResponse(
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Version: System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            Settings: settings);

        return Result<GetPlatformConfigResponse>.Success(response);
    }
}

/// <summary>
/// Platform role configuration detail.
/// </summary>
public sealed record PlatformRoleConfig(
    string Key,
    string Name,
    int HierarchyLevel,
    bool IsSystemRole,
    int ActiveAssignmentCount);