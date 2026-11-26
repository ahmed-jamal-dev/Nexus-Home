namespace NexusHome.Api.Authorization.Handlers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Api.Authorization.Requirements;

/// <summary>
/// Authorization handler for HasPermissionRequirement.
/// Checks if the current user has the required permission on the target.
/// Platform admins (SuperAdmin) bypass home membership requirement.
/// </summary>
public sealed class HasPermissionHandler : AuthorizationHandler<HasPermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly ILogger<HasPermissionHandler> _logger;

    public HasPermissionHandler(
        IHttpContextAccessor httpContextAccessor,
        IMembershipRepository membershipRepository,
        IPermissionRepository permissionRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        ILogger<HasPermissionHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _membershipRepository = membershipRepository;
        _permissionRepository = permissionRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _logger = logger;
        _logger.LogInformation("HasPermissionHandler: Constructor called");
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HasPermissionHandler: No HTTP context");
            return; // No HTTP context, cannot authorize
        }

        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("HasPermissionHandler: No user ID in claims");
            return; // No user ID in claims
        }

        // Get home ID from route
        var routeData = httpContext.GetRouteData();
        if (!routeData.Values.TryGetValue("homeId", out var homeIdObj) || !Guid.TryParse(homeIdObj?.ToString(), out var homeId))
        {
            // Try alternative route parameter names
            if (!routeData.Values.TryGetValue("id", out homeIdObj) || !Guid.TryParse(homeIdObj?.ToString(), out homeId))
            {
                _logger.LogWarning("HasPermissionHandler: No home ID in route. Route values: {RouteValues}", routeData.Values.Keys);
                return; // No home ID in route
            }
        }

        _logger.LogInformation("HasPermissionHandler: Checking permission {PermissionType} on {TargetType} for user {UserId} in home {HomeId}",
            requirement.PermissionType, requirement.TargetType, userId, homeId);

        // Check if user is PlatformAdmin or SuperAdmin (bypass home membership for platform operations)
        // Privacy-sensitive permissions (camera/microphone) always require explicit grant even for platform admins
        var isPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(userId);
        if (isPlatformAdmin && !requirement.PermissionType.IsPrivacySensitive())
        {
            _logger.LogInformation("HasPermissionHandler: User {UserId} is PlatformAdmin/SuperAdmin, succeeding requirement for non-privacy permission {PermissionType}", userId, requirement.PermissionType);
            context.Succeed(requirement);
            return;
        }

        // Get user's membership in this home
        var membership = await _membershipRepository.GetByUserAndHomeAsync(userId, homeId);
        if (membership == null)
        {
            _logger.LogWarning("HasPermissionHandler: No membership found for user {UserId} in home {HomeId}", userId, homeId);
            return; // Not a member of this home
        }

        _logger.LogInformation("HasPermissionHandler: Found membership {MembershipId} with role {Role}", membership.Id, membership.Role);

        // Check if user is Owner (owners have OwnerDefaults permissions implicitly)
        // Privacy-sensitive permissions (camera/microphone) always require explicit grant
        if (membership.Role == Role.Owner && requirement.PermissionType.IsOwnerDefault())
        {
            _logger.LogInformation("HasPermissionHandler: User is Owner with OwnerDefault permission {PermissionType}, succeeding requirement", requirement.PermissionType);
            context.Succeed(requirement);
            return;
        }

        // Get target ID from route
        if (!routeData.Values.TryGetValue(requirement.TargetIdRouteParameter, out var targetIdObj) ||
            !Guid.TryParse(targetIdObj?.ToString(), out var targetId))
        {
            // For home-level permissions, target might be the home itself
            if (requirement.TargetType == TargetType.Home)
            {
                targetId = homeId;
            }
            else
            {
                return; // Target ID not found
            }
        }

        // Check permission
        var hasPermission = await _permissionRepository.HasPermissionAsync(
            membership.Id,
            requirement.PermissionType,
            requirement.TargetType,
            targetId);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}