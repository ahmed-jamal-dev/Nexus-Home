namespace NexusHome.Application.Features.Home.Permission.ListPermissions;

using NexusHome.Domain.Enums;

/// <summary>
/// Response for ListPermissionsQuery.
/// </summary>
public sealed record ListPermissionsResponse(
    List<PermissionDto> Permissions,
    string? NextCursor);

/// <summary>
/// Permission DTO for listing.
/// </summary>
public sealed record PermissionDto(
    Guid PermissionId,
    Guid GranteeMembershipId,
    string GranteeUserId,
    string GranteeName,
    string GranteeRole,
    PermissionType PermissionType,
    TargetType TargetType,
    Guid TargetId,
    Effect Effect,
    Guid? GrantorMembershipId,
    string? GrantorUserId,
    string? GrantorName,
    DateTimeOffset GrantedAt);