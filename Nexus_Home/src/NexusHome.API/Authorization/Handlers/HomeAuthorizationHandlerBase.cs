namespace NexusHome.Api.Authorization.Handlers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NexusHome.Application.Common.Authorization;

/// <summary>
/// Base class for home-scoped authorization handlers.
/// Extracts homeId from route and userId from claims, then delegates to HomeAuthorizationService.
/// </summary>
public abstract class HomeAuthorizationHandlerBase<TRequirement> : AuthorizationHandler<TRequirement>
    where TRequirement : IAuthorizationRequirement
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly HomeAuthorizationService _authService;

    protected HomeAuthorizationHandlerBase(
        IHttpContextAccessor httpContextAccessor,
        HomeAuthorizationService authService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
    }

    protected Guid? GetHomeId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request.RouteValues.TryGetValue("homeId", out var value) == true
            && value is string homeIdStr
            && Guid.TryParse(homeIdStr, out var homeId))
        {
            return homeId;
        }
        return null;
    }

    protected Guid? GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        var userIdClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? user?.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TRequirement requirement)
    {
        var homeId = GetHomeId();
        var userId = GetUserId();

        if (homeId == null || userId == null)
        {
            // Cannot evaluate - let other handlers decide or fail
            return;
        }

        // AuthorizationHandlerContext doesn't provide CancellationToken directly.
        // Use HttpContext.RequestAborted if available, otherwise CancellationToken.None.
        var ct = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var isAuthorized = await CheckAuthorizationAsync(homeId.Value, userId.Value, ct);
        if (isAuthorized)
        {
            context.Succeed(requirement);
        }
    }

    protected abstract Task<bool> CheckAuthorizationAsync(Guid homeId, Guid userId, CancellationToken ct);
}