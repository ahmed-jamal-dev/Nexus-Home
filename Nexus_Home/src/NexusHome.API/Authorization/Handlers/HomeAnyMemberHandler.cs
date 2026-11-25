namespace NexusHome.Api.Authorization.Handlers;

using NexusHome.Api.Authorization.Requirements;
using NexusHome.Application.Common.Authorization;

/// <summary>
/// Handler for HomeAnyMemberRequirement - allows Owner, Member, or Guest role (with SuperAdmin/PlatformAdmin/Provisioner bypass).
/// </summary>
public sealed class HomeAnyMemberHandler : HomeAuthorizationHandlerBase<HomeAnyMemberRequirement>
{
    public HomeAnyMemberHandler(IHttpContextAccessor httpContextAccessor, HomeAuthorizationService authService)
        : base(httpContextAccessor, authService) { }

    protected override Task<bool> CheckAuthorizationAsync(Guid homeId, Guid userId, CancellationToken ct)
        => _authService.IsAnyMemberAsync(homeId, userId, ct);
}