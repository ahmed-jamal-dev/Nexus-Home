namespace NexusHome.Application.Features.Platform.Health;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get platform health/status.
/// </summary>
public sealed record GetPlatformHealthQuery(
    Guid RequesterUserId) : IRequest<Result<GetPlatformHealthResponse>>;

/// <summary>
/// Response for GetPlatformHealthQuery.
/// </summary>
public sealed record GetPlatformHealthResponse(
    int TotalUsers,
    int ActiveUsers,
    int TotalHomes,
    int ActiveHomes,
    int TotalDevices,
    int OnlineDevices,
    int TotalPlatformRoles,
    List<PlatformRoleSummary> PlatformRoleCounts);

/// <summary>
/// Platform role count summary.
/// </summary>
public sealed record PlatformRoleSummary(
    string RoleKey,
    string RoleName,
    int UserCount);