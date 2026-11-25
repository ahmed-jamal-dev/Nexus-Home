namespace NexusHome.Api.Authorization.Handlers;

using NexusHome.Api.Authorization.Requirements;
using NexusHome.Application.Common.Authorization;

/// <summary>
/// Handler for HomeOwnerRequirement - allows Owner role (with SuperAdmin/PlatformAdmin/Provisioner bypass).
/// </summary>
public sealed class HomeOwnerHandler : HomeAuthorizationHandlerBase<HomeOwnerRequirement>
{
    public HomeOwnerHandler(IHttpContextAccessor httpContextAccessor, HomeAuthorizationService authService)
        : base(httpContextAccessor, authService) { }

    protected override Task<bool> CheckAuthorizationAsync(Guid homeId, Guid userId, CancellationToken ct)
        => _authService.IsOwnerAsync(homeId, userId, ct);
}