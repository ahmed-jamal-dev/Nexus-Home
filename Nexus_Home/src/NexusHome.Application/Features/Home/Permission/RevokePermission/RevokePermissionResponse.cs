namespace NexusHome.Application.Features.Home.Permission.RevokePermission;

/// <summary>
/// Response for RevokePermissionCommand.
/// </summary>
public sealed record RevokePermissionResponse(
    Guid PermissionId,
    bool Revoked);