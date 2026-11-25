namespace NexusHome.Api.Authorization.Handlers;

using NexusHome.Api.Authorization.Requirements;
using NexusHome.Application.Common.Authorization;

/// <summary>
/// Handler for HomeMemberOrOwnerRequirement - allows Member or Owner role (with SuperAdmin/PlatformAdmin/Provisioner bypass).
/// </summary>
public sealed class HomeMemberOrOwnerHandler : HomeAuthorizationHandlerBase<HomeMemberOrOwnerRequirement>
{
    public HomeMemberOrOwnerHandler(IHttpContextAccessor httpContextAccessor, HomeAuthorizationService authService)
        : base(httpContextAccessor, authService) { }

    protected override Task<bool> CheckAuthorizationAsync(Guid homeId, Guid userId, CancellationToken ct)
        => _authService.IsMemberOrOwnerAsync(homeId, userId, ct);
}