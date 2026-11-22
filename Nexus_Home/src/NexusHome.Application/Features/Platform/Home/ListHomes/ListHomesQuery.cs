namespace NexusHome.Application.Features.Platform.Home.ListHomes;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list homes with pagination and filtering (platform admin view).
/// </summary>
public sealed record ListHomesQuery(
    Guid RequesterUserId,
    string? Search = null,
    bool? IsDeleted = null,
    string? Cursor = null,
    int Limit = 20) : IRequest<Result<ListHomesResponse>>;

/// <summary>
/// Response for ListHomesQuery.
/// </summary>
public sealed record ListHomesResponse(
    List<HomeListItem> Homes,
    string? NextCursor,
    int TotalCount);

/// <summary>
/// Home list item for platform administration.
/// </summary>
public sealed record HomeListItem(
    Guid Id,
    string Name,
    string? City,
    string? StateProvince,
    string? Country,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    Guid? OwnerId,
    string? OwnerEmail,
    Guid? ProvisionedByTechnicianId,
    int MemberCount,
    int DeviceCount);