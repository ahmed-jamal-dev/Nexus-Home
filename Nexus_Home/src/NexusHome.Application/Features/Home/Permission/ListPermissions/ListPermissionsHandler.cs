namespace NexusHome.Application.Features.Home.Permission.ListPermissions;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for ListPermissionsQuery.
/// </summary>
public sealed class ListPermissionsHandler : IRequestHandler<ListPermissionsQuery, Result<ListPermissionsResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public ListPermissionsHandler(
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IPermissionRepository permissionRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _permissionRepository = permissionRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<ListPermissionsResponse>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<ListPermissionsResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Check if user has MemberManage permission or is Owner
        var requesterMembership = await _membershipRepository.GetByUserAndHomeAsync(request.UserId, home.Id, cancellationToken);
        if (requesterMembership == null)
        {
            return Result<ListPermissionsResponse>.Failure(Errors.PermissionErrors.MemberNotFound);
        }

        var hasMemberManage = await _permissionRepository.HasPermissionAsync(
            requesterMembership.Id,
            PermissionType.MemberManage,
            TargetType.Home,
            home.Id,
            cancellationToken);

        if (!hasMemberManage && requesterMembership.Role != Role.Owner)
        {
            return Result<ListPermissionsResponse>.Failure(Errors.PermissionErrors.PermissionDenied);
        }

        // Parse target type if provided
        TargetType? targetType = null;
        if (!string.IsNullOrEmpty(request.TargetType) &&
            Enum.TryParse<TargetType>(request.TargetType, true, out var parsedTargetType))
        {
            targetType = parsedTargetType;
        }

        // Get permissions with details
        var permissions = await _permissionRepository.ListByHomeWithDetailsAsync(
            home.Id,
            request.GranteeMembershipId,
            targetType,
            request.TargetId,
            request.Cursor,
            request.Limit,
            cancellationToken);

        // Build response DTOs (take only the requested limit)
        var permissionDtos = permissions
            .Take(request.Limit)
            .Select(p => new PermissionDto(
                p.Id,
                p.GranteeMembershipId,
                p.GranteeMembership.UserId.ToString(),
                p.GranteeMembership.User?.FullName ?? "Unknown",
                p.GranteeMembership.Role.ToString(),
                p.PermissionType,
                p.TargetType,
                p.TargetId,
                p.Effect,
                p.GrantorMembershipId,
                p.GrantorMembership?.UserId.ToString(),
                p.GrantorMembership?.User?.FullName,
                p.GrantedAt))
            .ToList();

        // Determine next cursor
        string? nextCursor = null;
        if (permissions.Count > request.Limit)
        {
            var last = permissions[request.Limit - 1];
            var cursorString = $"{last.CreatedAt:O}|{last.Id}";
            var cursorBytes = System.Text.Encoding.UTF8.GetBytes(cursorString);
            nextCursor = Convert.ToBase64String(cursorBytes);
        }

        var response = new ListPermissionsResponse(permissionDtos, nextCursor);

        return Result<ListPermissionsResponse>.Success(response);
    }
}