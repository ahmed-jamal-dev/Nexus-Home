namespace NexusHome.Application.Features.Platform.Home.GetHome;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get a home by ID (platform admin view).
/// </summary>
public sealed record GetHomeQuery(
    Guid RequesterUserId,
    Guid HomeId) : IRequest<Result<GetHomeResponse>>;

/// <summary>
/// Response for GetHomeQuery.
/// </summary>
public sealed record GetHomeResponse(
    HomeDetail Home);

/// <summary>
/// Detailed home information for platform administration.
/// </summary>
public sealed record HomeDetail(
    Guid Id,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string Timezone,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeletedAt,
    Guid? OwnerId,
    string? OwnerEmail,
    Guid? ProvisionedByTechnicianId,
    List<HomeMembershipDetail> Memberships,
    List<HomeDeviceSummary> Devices,
    List<HomeRoomSummary> Rooms);

/// <summary>
/// Home membership detail for platform administration.
/// </summary>
public sealed record HomeMembershipDetail(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset JoinedAt);

/// <summary>
/// Home device summary.
/// </summary>
public sealed record HomeDeviceSummary(
    Guid Id,
    string Name,
    string Type,
    string? RoomName,
    bool IsOnline,
    DateTimeOffset? LastSeenAt);

/// <summary>
/// Home room summary.
/// </summary>
public sealed record HomeRoomSummary(
    Guid Id,
    string Name,
    int DeviceCount);