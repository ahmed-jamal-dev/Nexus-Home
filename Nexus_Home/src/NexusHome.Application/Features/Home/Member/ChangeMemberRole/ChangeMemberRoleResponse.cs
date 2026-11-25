namespace NexusHome.Application.Features.Home.Member.ChangeMemberRole;

/// <summary>
/// Response for ChangeMemberRoleCommand.
/// </summary>
public sealed record ChangeMemberRoleResponse(
    Guid MembershipId,
    Guid HomeId,
    Guid UserId,
    string Role);