namespace NexusHome.Application.Features.Platform.User.UpdateUserStatus;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for UpdateUserStatusCommand.
/// </summary>
public sealed class UpdateUserStatusHandler : IRequestHandler<UpdateUserStatusCommand, Result<UpdateUserStatusResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public UpdateUserStatusHandler(
        IUserRepository userRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<UpdateUserStatusResponse>> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        // Prevent self-deactivation
        if (request.RequesterUserId == request.TargetUserId)
        {
            return Result<UpdateUserStatusResponse>.Failure(Platform.CannotDeactivateSelf);
        }

        // Verify requester has PlatformAdmin or higher
        var hasPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.RequesterUserId, cancellationToken);
        if (!hasPlatformAdmin)
        {
            return Result<UpdateUserStatusResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get target user
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (targetUser == null)
        {
            return Result<UpdateUserStatusResponse>.Failure(Platform.UserNotFound);
        }

        // Check if target user is SuperAdmin
        var isTargetSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.TargetUserId, "SuperAdmin", cancellationToken: cancellationToken);
        if (isTargetSuperAdmin)
        {
            // Allow SuperAdmin to modify other SuperAdmin users
            var isRequesterSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.RequesterUserId, "SuperAdmin", cancellationToken: cancellationToken);
            if (!isRequesterSuperAdmin)
            {
                return Result<UpdateUserStatusResponse>.Failure(Platform.CannotModifySuperAdmin);
            }
        }

        // Update status
        if (request.IsActive)
        {
            targetUser.Activate();
        }
        else
        {
            targetUser.Deactivate();
        }

        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Invalidate token version
        targetUser.IncrementPlatformRolesVersion();
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            request.IsActive ? "UserActivated" : "UserDeactivated",
            "User",
            request.TargetUserId,
            $"{{\"isActive\":{request.IsActive.ToString().ToLowerInvariant()}}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<UpdateUserStatusResponse>.Success(new UpdateUserStatusResponse(
            targetUser.Id,
            targetUser.IsActive));
    }
}