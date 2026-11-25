namespace NexusHome.Api.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization requirement for any home membership (Owner, Member, or Guest).
/// </summary>
public sealed class HomeAnyMemberRequirement : IAuthorizationRequirement { }