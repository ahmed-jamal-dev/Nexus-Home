namespace NexusHome.Application.Features.Platform.Config.GetPlatformConfig;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get platform configuration (SuperAdmin only).
/// </summary>
public sealed record GetPlatformConfigQuery(Guid RequesterUserId) : IRequest<Result<GetPlatformConfigResponse>>;

/// <summary>
/// Response for platform configuration.
/// </summary>
public sealed record GetPlatformConfigResponse(
    string Environment,
    string Version,
    Dictionary<string, string> Settings);