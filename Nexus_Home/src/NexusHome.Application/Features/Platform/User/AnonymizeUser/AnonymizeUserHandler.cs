namespace NexusHome.Application.Features.Platform.User.AnonymizeUser;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for AnonymizeUserCommand.
/// </summary>
public sealed class AnonymizeUserHandler : IRequestHandler<AnonymizeUserCommand, Result<AnonymizeUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public AnonymizeUserHandler(
        IUserRepository userRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<AnonymizeUserResponse>> Handle(AnonymizeUserCommand request, CancellationToken cancellationToken)
    {
        // Prevent self-anonymization
        if (request.RequesterUserId == request.TargetUserId)
        {
            return Result<AnonymizeUserResponse>.Failure(Platform.CannotDeactivateSelf);
        }

        // Verify requester has SuperAdmin role (only SuperAdmin can anonymize users)
        var isRequesterSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.RequesterUserId, "SuperAdmin", cancellationToken: cancellationToken);
        if (!isRequesterSuperAdmin)
        {
            return Result<AnonymizeUserResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get target user
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (targetUser == null)
        {
            return Result<AnonymizeUserResponse>.Failure(Platform.UserNotFound);
        }

        // Check if target user is SuperAdmin
        var isTargetSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.TargetUserId, "SuperAdmin", cancellationToken: cancellationToken);
        if (isTargetSuperAdmin)
        {
            return Result<AnonymizeUserResponse>.Failure(Platform.CannotModifySuperAdmin);
        }

        // Store original email for response
        var originalEmail = targetUser.Email;

        // Anonymize user (GDPR right to erasure)
        targetUser.Anonymize();

        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Invalidate token version
        targetUser.IncrementPlatformRolesVersion();
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            "UserAnonymized",
            "User",
            request.TargetUserId,
            $"{{\"originalEmail\":\"{originalEmail}\"}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<AnonymizeUserResponse>.Success(new AnonymizeUserResponse(
            targetUser.Id,
            targetUser.Email,
            targetUser.IsActive));
    }
}