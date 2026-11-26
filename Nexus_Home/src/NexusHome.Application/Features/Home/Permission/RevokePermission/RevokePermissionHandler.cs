namespace NexusHome.Application.Features.Home.Permission.RevokePermission;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Events;
using NexusHome.Domain.Enums;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for RevokePermissionCommand.
/// </summary>
public sealed class RevokePermissionHandler : IRequestHandler<RevokePermissionCommand, Result<RevokePermissionResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public RevokePermissionHandler(
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IPermissionRepository permissionRepository,
        IOutboxEventRepository outboxEventRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _permissionRepository = permissionRepository;
        _outboxEventRepository = outboxEventRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<RevokePermissionResponse>> Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<RevokePermissionResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Get the permission
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission == null)
        {
            return Result<RevokePermissionResponse>.Failure(Errors.PermissionErrors.PermissionNotFound);
        }

        // Verify permission belongs to this home
        if (permission.HomeId != home.Id)
        {
            return Result<RevokePermissionResponse>.Failure(Errors.PermissionErrors.PermissionNotFound);
        }

        // Get requester's membership
        var requesterMembership = await _membershipRepository.GetByUserAndHomeAsync(request.UserId, home.Id, cancellationToken);
        if (requesterMembership == null)
        {
            return Result<RevokePermissionResponse>.Failure(Errors.PermissionErrors.MemberNotFound);
        }

        // Check if requester has MemberManage permission or is Owner
        var hasMemberManage = await _permissionRepository.HasPermissionAsync(
            requesterMembership.Id,
            PermissionType.MemberManage,
            TargetType.Home,
            home.Id,
            cancellationToken);

        if (!hasMemberManage && requesterMembership.Role != Role.Owner)
        {
            return Result<RevokePermissionResponse>.Failure(Errors.PermissionErrors.PermissionDenied);
        }

        // Cannot revoke Owner's permissions
        var granteeMembership = await _membershipRepository.GetByIdAsync(permission.GranteeMembershipId, cancellationToken);
        if (granteeMembership != null && granteeMembership.Role == Role.Owner)
        {
            return Result<RevokePermissionResponse>.Failure(Errors.PermissionErrors.OwnerCannotBeDenied);
        }

        // Store info for event
        var granteeId = permission.GranteeMembershipId;
        var permType = permission.PermissionType;
        var targetType = permission.TargetType;
        var targetId = permission.TargetId;
        var effect = permission.Effect;

        // Delete permission
        await _permissionRepository.DeleteAsync(permission, cancellationToken);

        // Publish permission.revoked outbox event
        var permissionEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "permission.revoked",
            Payload = JsonSerializer.Serialize(new
            {
                permissionId = request.PermissionId,
                homeId = home.Id,
                grantorMembershipId = permission.GrantorMembershipId,
                granteeMembershipId = granteeId,
                permissionType = permType.ToString(),
                targetType = targetType.ToString(),
                targetId = targetId,
                effect = effect.ToString(),
                revokedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = home.Id.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(permissionEvent, cancellationToken);

        var response = new RevokePermissionResponse(request.PermissionId, true);

        return Result<RevokePermissionResponse>.Success(response);
    }
}