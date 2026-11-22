namespace NexusHome.Application.Features.Platform.User.RevokePlatformRole;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for RevokePlatformRoleCommand.
/// </summary>
public sealed class RevokePlatformRoleHandler : IRequestHandler<RevokePlatformRoleCommand, Result<RevokePlatformRoleResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPlatformRoleRepository _platformRoleRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public RevokePlatformRoleHandler(
        IUserRepository userRepository,
        IPlatformRoleRepository platformRoleRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _platformRoleRepository = platformRoleRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<RevokePlatformRoleResponse>> Handle(RevokePlatformRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher
        var hasPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.RequesterUserId, cancellationToken);
        if (!hasPlatformAdmin)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get target user
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (targetUser == null)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.UserNotFound);
        }

        // Check if target user is SuperAdmin
        var isSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.TargetUserId, "SuperAdmin", cancellationToken: cancellationToken);
        if (isSuperAdmin)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.CannotModifySuperAdmin);
        }

        // Get role
        var role = await _platformRoleRepository.GetByKeyAsync(request.RoleKey, cancellationToken);
        if (role == null)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.RoleNotFound);
        }

        // Check if requester has higher hierarchy than the role being revoked
        var requesterRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(request.RequesterUserId, cancellationToken);
        var requesterMaxHierarchy = requesterRoles.Max(r => r.PlatformRole.HierarchyLevel);
        if (role.HierarchyLevel > requesterMaxHierarchy)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Check if it's a system role and target is SuperAdmin (already checked above)
        // but also prevent revoking SuperAdmin role from anyone by non-SuperAdmin
        if (role.Key == "SuperAdmin")
        {
            var requesterIsSuperAdmin = requesterRoles.Any(r => r.PlatformRole.Key == "SuperAdmin");
            if (!requesterIsSuperAdmin)
            {
                return Result<RevokePlatformRoleResponse>.Failure(Platform.CannotRevokeSystemRole);
            }
        }

        // Revoke role
        var revoked = await _userPlatformRoleRepository.RevokeAsync(
            request.TargetUserId,
            role.Id,
            request.ScopeId,
            request.ScopeType,
            cancellationToken);

        if (!revoked)
        {
            return Result<RevokePlatformRoleResponse>.Failure(Platform.RoleAssignmentNotFound);
        }

        // Invalidate target user's token version
        targetUser.IncrementPlatformRolesVersion();
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            "RoleRevoked",
            "User",
            request.TargetUserId,
            $"{{\"roleKey\":\"{request.RoleKey}\",\"scopeId\":\"{request.ScopeId}\",\"scopeType\":\"{request.ScopeType}\"}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<RevokePlatformRoleResponse>.Success(new RevokePlatformRoleResponse(
            request.TargetUserId,
            request.RoleKey,
            true));
    }
}