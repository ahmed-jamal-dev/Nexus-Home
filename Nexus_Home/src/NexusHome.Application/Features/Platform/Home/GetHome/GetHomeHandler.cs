namespace NexusHome.Application.Features.Platform.Home.GetHome;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for GetHomeQuery.
/// </summary>
public sealed class GetHomeHandler : IRequestHandler<GetHomeQuery, Result<GetHomeResponse>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IHomeRepository _homeRepository;

    public GetHomeHandler(
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IHomeRepository homeRepository)
    {
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _homeRepository = homeRepository;
    }

    public async Task<Result<GetHomeResponse>> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher (SupportAgent deferred to Phase 1.5)
        var requesterRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(request.RequesterUserId, cancellationToken);

        var hasPlatformRole = requesterRoles.Any(r =>
            r.PlatformRole.Key == "SuperAdmin" ||
            r.PlatformRole.Key == "PlatformAdmin");

        if (!hasPlatformRole)
        {
            return Result<GetHomeResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get Technician scope if applicable
        var technicianScope = requesterRoles
            .Where(r => r.PlatformRole.Key == "Technician" && r.ScopeId.HasValue)
            .Select(r => r.ScopeId!.Value)
            .ToList();

        var isPlatformAdminOrHigher = requesterRoles.Any(r =>
            r.PlatformRole.Key == "SuperAdmin" ||
            r.PlatformRole.Key == "PlatformAdmin");

        // Get home with details
        var homeDetails = await _homeRepository.GetHomeWithDetailsAsync(request.HomeId, cancellationToken);

        if (homeDetails == null)
        {
            return Result<GetHomeResponse>.Failure(Platform.HomeNotFound);
        }

        var (home, ownerEmail, _, _, provisionedByTechnicianId) = homeDetails.Value;

        // Check Technician scope
        if (technicianScope.Count > 0 && !isPlatformAdminOrHigher)
        {
            if (!technicianScope.Contains(home.Id))
            {
                return Result<GetHomeResponse>.Failure(Platform.InsufficientPrivilege);
            }
        }

        // Get memberships
        var memberships = await _homeRepository.GetHomeMembershipsAsync(request.HomeId, cancellationToken);

        var membershipDetails = memberships.Select(m => new HomeMembershipDetail(
            m.Membership.UserId,
            m.UserEmail,
            m.Membership.User?.FullName ?? "Unknown",
            m.Membership.Role.ToString(),
            m.Membership.CreatedAt
        )).ToList();

        // Get devices
        var devices = await _homeRepository.GetHomeDevicesAsync(request.HomeId, cancellationToken);

        var deviceSummaries = devices.Select(d => new HomeDeviceSummary(
            d.Id,
            d.Name,
            d.DeviceType,
            d.Room?.Name,
            d.ConnectionStatus == ConnectionStatus.Online,
            d.LastSeenAt
        )).ToList();

        // Get rooms
        var rooms = await _homeRepository.GetHomeRoomsAsync(request.HomeId, cancellationToken);

        var roomSummaries = new List<HomeRoomSummary>();
        foreach (var room in rooms)
        {
            var deviceCount = devices.Count(d => d.RoomId == room.Id);
            roomSummaries.Add(new HomeRoomSummary(
                room.Id,
                room.Name,
                deviceCount));
        }

        // Get owner
        var ownerMembership = memberships.FirstOrDefault(m => m.Membership.Role == Role.Owner);
        var hasOwner = ownerMembership.Membership != null;
        var ownerId = hasOwner ? ownerMembership.Membership!.UserId : (Guid?)null;
        var ownerEmailFinal = hasOwner ? ownerMembership.UserEmail : null;

        var homeDetail = new HomeDetail(
            home.Id,
            home.Name,
            home.AddressLine1,
            home.AddressLine2,
            home.City,
            home.StateProvince,
            home.PostalCode,
            home.Timezone,
            home.DeletedAt != null,
            home.CreatedAt,
            home.DeletedAt,
            ownerId,
            ownerEmailFinal,
            provisionedByTechnicianId,
            membershipDetails,
            deviceSummaries,
            roomSummaries
        );

        return Result<GetHomeResponse>.Success(new GetHomeResponse(homeDetail));
    }
}