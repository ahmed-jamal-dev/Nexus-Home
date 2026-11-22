namespace NexusHome.Application.Features.Platform.Home.GetHomeMemberships;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get home memberships (platform admin view).
/// </summary>
public sealed record GetHomeMembershipsQuery(
    Guid RequesterUserId,
    Guid HomeId) : IRequest<Result<GetHomeMembershipsResponse>>;

/// <summary>
/// Response for GetHomeMembershipsQuery.
/// </summary>
public sealed record GetHomeMembershipsResponse(
    List<HomeMembershipDetail> Memberships);

/// <summary>
/// Home membership detail for platform administration.
/// </summary>
public sealed record HomeMembershipDetail(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset JoinedAt,
    List<string> PlatformRoles);