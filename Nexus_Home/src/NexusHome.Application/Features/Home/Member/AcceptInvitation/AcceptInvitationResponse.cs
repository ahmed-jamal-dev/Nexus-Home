namespace NexusHome.Application.Features.Home.Member.AcceptInvitation;

/// <summary>
/// Response for AcceptInvitationCommand.
/// </summary>
public sealed record AcceptInvitationResponse(
    Guid MembershipId,
    Guid HomeId,
    string Role,
    DateTimeOffset JoinedAt);