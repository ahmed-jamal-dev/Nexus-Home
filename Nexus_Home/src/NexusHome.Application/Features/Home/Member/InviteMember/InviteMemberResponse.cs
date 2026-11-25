namespace NexusHome.Application.Features.Home.Member.InviteMember;

/// <summary>
/// Response for InviteMemberCommand.
/// </summary>
public sealed record InviteMemberResponse(
    Guid InvitationId,
    string Email,
    string Role,
    DateTimeOffset ExpiresAt,
    string PlainToken);