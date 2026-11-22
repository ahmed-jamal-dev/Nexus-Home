namespace NexusHome.Application.Features.Platform.User.AssignPlatformRole;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for AssignPlatformRoleCommand.
/// </summary>
public sealed class AssignPlatformRoleHandler : IRequestHandler<AssignPlatformRoleCommand, Result<AssignPlatformRoleResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IPlatformRoleRepository _platformRoleRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public AssignPlatformRoleHandler(
        IUserRepository userRepository,
        IHomeRepository homeRepository,
        IPlatformRoleRepository platformRoleRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _homeRepository = homeRepository;
        _platformRoleRepository = platformRoleRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<AssignPlatformRoleResponse>> Handle(AssignPlatformRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher
        var hasPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.RequesterUserId, cancellationToken);
        if (!hasPlatformAdmin)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Get target user
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (targetUser == null)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.UserNotFound);
        }

        // Check if target user is SuperAdmin (cannot modify SuperAdmin roles)
        var isSuperAdmin = await _userPlatformRoleRepository.HasRoleAsync(request.TargetUserId, "SuperAdmin", cancellationToken: cancellationToken);
        if (isSuperAdmin)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.CannotModifySuperAdmin);
        }

        // Get role
        var role = await _platformRoleRepository.GetByKeyAsync(request.RoleKey, cancellationToken);
        if (role == null)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.RoleNotFound);
        }

        // Check if requester has higher hierarchy than the role being assigned
        var requesterRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(request.RequesterUserId, cancellationToken);
        var requesterMaxHierarchy = requesterRoles.Max(r => r.PlatformRole.HierarchyLevel);
        if (role.HierarchyLevel > requesterMaxHierarchy)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Validate scope for Technician role
        if (role.Key == "Technician")
        {
            if (!request.ScopeId.HasValue || string.IsNullOrEmpty(request.ScopeType))
            {
                return Result<AssignPlatformRoleResponse>.Failure(Platform.ScopeRequired);
            }

            if (request.ScopeType != "Home")
            {
                return Result<AssignPlatformRoleResponse>.Failure(Platform.InvalidScope);
            }

            // Verify home exists
            var home = await _homeRepository.GetByIdAsync(request.ScopeId.Value, cancellationToken);
            if (home == null)
            {
                return Result<AssignPlatformRoleResponse>.Failure(Platform.HomeNotFound);
            }
        }

        // Check if already assigned
        var alreadyAssigned = await _userPlatformRoleRepository.HasRoleAsync(
            request.TargetUserId,
            request.RoleKey,
            request.ScopeId,
            request.ScopeType,
            cancellationToken);

        if (alreadyAssigned)
        {
            return Result<AssignPlatformRoleResponse>.Failure(Platform.RoleAlreadyAssigned);
        }

        // Assign role
        var userPlatformRole = UserPlatformRole.Create(
            request.TargetUserId,
            role.Id,
            request.RequesterUserId,
            request.ScopeId,
            request.ScopeType,
            request.ExpiresAt);

        await _userPlatformRoleRepository.AddAsync(userPlatformRole, cancellationToken);

        // Invalidate target user's token version
        targetUser.IncrementPlatformRolesVersion();
        await _userRepository.UpdateAsync(targetUser, cancellationToken);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            "RoleAssigned",
            "User",
            request.TargetUserId,
            $"{{\"roleKey\":\"{request.RoleKey}\",\"scopeId\":\"{request.ScopeId}\",\"scopeType\":\"{request.ScopeType}\"}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<AssignPlatformRoleResponse>.Success(new AssignPlatformRoleResponse(
            request.TargetUserId,
            request.RoleKey,
            request.ScopeId,
            request.ScopeType));
    }
}