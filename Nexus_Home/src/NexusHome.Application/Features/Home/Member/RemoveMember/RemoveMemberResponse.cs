namespace NexusHome.Application.Features.Home.Member.RemoveMember;

/// <summary>
/// Response for RemoveMemberCommand.
/// </summary>
public sealed record RemoveMemberResponse(
    Guid MembershipId,
    Guid HomeId,
    Guid UserId);