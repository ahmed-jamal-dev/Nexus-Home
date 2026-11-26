namespace NexusHome.Application.Features.Home.Permission.GrantPermission;

using NexusHome.Domain.Enums;

/// <summary>
/// Response for GrantPermissionCommand.
/// </summary>
public sealed record GrantPermissionResponse(
    Guid PermissionId,
    Guid GranteeMembershipId,
    PermissionType PermissionType,
    TargetType TargetType,
    Guid TargetId,
    Effect Effect,
    DateTimeOffset GrantedAt);