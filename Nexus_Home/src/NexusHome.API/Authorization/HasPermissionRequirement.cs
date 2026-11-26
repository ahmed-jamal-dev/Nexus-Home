namespace NexusHome.Api.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;
using NexusHome.Domain.Enums;

/// <summary>
/// Authorization requirement for checking a specific permission on a target.
/// </summary>
public sealed class HasPermissionRequirement : IAuthorizationRequirement
{
    public HasPermissionRequirement(
        PermissionType permissionType,
        TargetType targetType,
        string targetIdRouteParameter = "targetId")
    {
        PermissionType = permissionType;
        TargetType = targetType;
        TargetIdRouteParameter = targetIdRouteParameter;
    }

    public PermissionType PermissionType { get; }
    public TargetType TargetType { get; }
    public string TargetIdRouteParameter { get; }
}