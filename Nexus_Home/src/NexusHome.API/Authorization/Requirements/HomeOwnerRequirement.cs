namespace NexusHome.Api.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement for home Owner role.
/// </summary>
public sealed class HomeOwnerRequirement : IAuthorizationRequirement { }