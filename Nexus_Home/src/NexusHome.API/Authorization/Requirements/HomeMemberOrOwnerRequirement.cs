namespace NexusHome.Api.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement for home Member or Owner role.
/// </summary>
public sealed class HomeMemberOrOwnerRequirement : IAuthorizationRequirement { }