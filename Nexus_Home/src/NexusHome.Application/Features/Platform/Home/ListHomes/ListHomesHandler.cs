namespace NexusHome.Application.Features.Platform.Home.ListHomes;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for ListHomesQuery.
/// </summary>
public sealed class ListHomesHandler : IRequestHandler<ListHomesQuery, Result<ListHomesResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IHomeRepository _homeRepository;

    public ListHomesHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IHomeRepository homeRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _homeRepository = homeRepository;
    }

    public async Task<Result<ListHomesResponse>> Handle(ListHomesQuery request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher (SupportAgent deferred to Phase 1.5)
        var requesterRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(request.RequesterUserId, cancellationToken);

        var hasPlatformRole = requesterRoles.Any(r =>
            r.PlatformRole.Key == "SuperAdmin" ||
            r.PlatformRole.Key == "PlatformAdmin");

        if (!hasPlatformRole)
        {
            return Result<ListHomesResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get Technician scope if applicable
        var technicianScope = requesterRoles
            .Where(r => r.PlatformRole.Key == "Technician" && r.ScopeId.HasValue)
            .Select(r => r.ScopeId!.Value)
            .ToList();

        var isPlatformAdminOrHigher = requesterRoles.Any(r =>
            r.PlatformRole.Key == "SuperAdmin" ||
            r.PlatformRole.Key == "PlatformAdmin");

        // Get total count
        var totalCount = await _homeRepository.GetHomesCountAsync(
            request.Search,
            request.IsDeleted,
            technicianScope,
            isPlatformAdminOrHigher,
            cancellationToken);

        // Get paginated results
        var homes = await _homeRepository.GetHomesAsync(
            request.Search,
            request.IsDeleted,
            technicianScope,
            isPlatformAdminOrHigher,
            request.Cursor,
            request.Limit,
            cancellationToken);

        var homeItems = homes.Take(request.Limit).Select(h => new HomeListItem(
            h.Home.Id,
            h.Home.Name,
            h.Home.City,
            h.Home.StateProvince,
            h.Home.PostalCode, // Using PostalCode as Country proxy
            h.Home.DeletedAt != null,
            h.Home.CreatedAt,
            h.Home.Memberships.FirstOrDefault(m => m.HomeId == h.Home.Id && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner)?.UserId,
            h.OwnerEmail,
            h.ProvisionedByTechnicianId,
            h.MemberCount,
            h.DeviceCount
        )).ToList();

        string? nextCursor = null;
        if (homes.Count > request.Limit)
        {
            nextCursor = homes[request.Limit - 1].Home.Id.ToString();
        }

        return Result<ListHomesResponse>.Success(new ListHomesResponse(homeItems, nextCursor, totalCount));
    }
}