namespace NexusHome.Application.Features.Home.Permission.GrantPermission;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Events;
using NexusHome.Domain.Enums;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for GrantPermissionCommand.
/// </summary>
public sealed class GrantPermissionHandler : IRequestHandler<GrantPermissionCommand, Result<GrantPermissionResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public GrantPermissionHandler(
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IPermissionRepository permissionRepository,
        IDeviceRepository deviceRepository,
        IRoomRepository roomRepository,
        IOutboxEventRepository outboxEventRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _permissionRepository = permissionRepository;
        _deviceRepository = deviceRepository;
        _roomRepository = roomRepository;
        _outboxEventRepository = outboxEventRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<GrantPermissionResponse>> Handle(GrantPermissionCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (must be Owner or Member with MemberManage permission)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<GrantPermissionResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Check if user has MemberManage permission on this home
        var membershipResult = await _membershipRepository.GetByUserAndHomeAsync(request.UserId, home.Id, cancellationToken);
        if (membershipResult == null)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.MemberNotFound);
        }

        // Check MemberManage permission on home
        var hasMemberManage = await _permissionRepository.HasPermissionAsync(
            membershipResult.Id,
            PermissionType.MemberManage,
            TargetType.Home,
            home.Id,
            cancellationToken);

        if (!hasMemberManage && membershipResult.Role != Role.Owner)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.PermissionDenied);
        }

        // Validate grantee membership exists and belongs to same home
        var granteeMembership = await _membershipRepository.GetByIdAsync(request.GranteeMembershipId, cancellationToken);
        if (granteeMembership == null || granteeMembership.HomeId != home.Id)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.MemberNotFound);
        }

        // Cannot grant permission to yourself
        if (granteeMembership.Id == membershipResult.Id)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.CannotGrantSelf);
        }

        // Parse enums
        if (!PermissionTypeExtensions.TryParseFromPgLabel(request.PermissionType, out var permissionType))
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.InvalidPermissionType);
        }

        if (!TargetTypeExtensions.TryParseFromPgLabel(request.TargetType, out var targetType))
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.InvalidTargetType);
        }

        if (!Enum.TryParse<Effect>(request.Effect, true, out var effect))
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.InvalidEffect);
        }

        // Cannot deny Owner
        if (effect == Effect.Deny && granteeMembership.Role == Role.Owner)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.OwnerCannotBeDenied);
        }

        // Validate target exists and belongs to this home
        var targetValidation = await ValidateTargetAsync(home.Id, targetType, request.TargetId, permissionType, cancellationToken);
        if (targetValidation.IsFailure)
        {
            return Result<GrantPermissionResponse>.Failure(targetValidation.Error);
        }

        // Check if permission already exists
        var exists = await _permissionRepository.ExistsAsync(
            granteeMembership.Id,
            permissionType,
            targetType,
            request.TargetId,
            effect,
            cancellationToken);

        if (exists)
        {
            return Result<GrantPermissionResponse>.Failure(Errors.PermissionErrors.PermissionAlreadyExists);
        }

        // Create permission
        var permission = NexusHome.Domain.Entities.Permission.Create(
            granteeMembership.Id,
            membershipResult.Id,
            permissionType,
            targetType,
            request.TargetId,
            effect,
            home.Id);

        await _permissionRepository.AddAsync(permission, cancellationToken);

        // Publish permission.granted outbox event
        var permissionEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "permission.granted",
            Payload = JsonSerializer.Serialize(new
            {
                permissionId = permission.Id,
                homeId = home.Id,
                grantorMembershipId = membershipResult.Id,
                granteeMembershipId = granteeMembership.Id,
                permissionType = permissionType.ToString(),
                targetType = targetType.ToString(),
                targetId = request.TargetId,
                effect = effect.ToString(),
                grantedAt = permission.GrantedAt
            }),
            CorrelationId = home.Id.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(permissionEvent, cancellationToken);

        var response = new GrantPermissionResponse(
            permission.Id,
            granteeMembership.Id,
            permissionType,
            targetType,
            request.TargetId,
            effect,
            permission.GrantedAt);

        return Result<GrantPermissionResponse>.Success(response);
    }

    private async Task<Result> ValidateTargetAsync(
        Guid homeId,
        TargetType targetType,
        Guid targetId,
        PermissionType permissionType,
        CancellationToken cancellationToken)
    {
        switch (targetType)
        {
            case TargetType.Device:
                var device = await _deviceRepository.GetByIdAsync(targetId, cancellationToken);
                if (device == null || device.HomeId != homeId)
                {
                    return Result.Failure(Errors.PermissionErrors.TargetNotFound);
                }
                break;

            case TargetType.Home:
                if (targetId != homeId)
                {
                    return Result.Failure(Errors.PermissionErrors.CrossHomeTarget);
                }
                break;

            case TargetType.Room:
                var room = await _roomRepository.GetByIdAsync(targetId, cancellationToken);
                if (room == null || room.HomeId != homeId)
                {
                    return Result.Failure(Errors.PermissionErrors.TargetNotFound);
                }
                break;

            case TargetType.Camera:
                var cameraDevice = await _deviceRepository.GetByIdAsync(targetId, cancellationToken);
                if (cameraDevice == null || cameraDevice.HomeId != homeId)
                {
                    return Result.Failure(Errors.PermissionErrors.TargetNotFound);
                }
                if (cameraDevice.Capabilities?.GetValueOrDefault("camera")?.ToString()?.ToLowerInvariant() != "true")
                {
                    return Result.Failure(Errors.PermissionErrors.CameraRequired);
                }
                break;

            case TargetType.Microphone:
                var micDevice = await _deviceRepository.GetByIdAsync(targetId, cancellationToken);
                if (micDevice == null || micDevice.HomeId != homeId)
                {
                    return Result.Failure(Errors.PermissionErrors.TargetNotFound);
                }
                if (micDevice.Capabilities?.GetValueOrDefault("microphone")?.ToString()?.ToLowerInvariant() != "true")
                {
                    return Result.Failure(Errors.PermissionErrors.MicrophoneRequired);
                }
                break;

            default:
                return Result.Failure(Errors.PermissionErrors.InvalidTargetType);
        }

        return Result.Success();
    }
}