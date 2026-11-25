namespace NexusHome.Application.Features.Home.Member.RemoveMember;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to remove a member from a home.
/// </summary>
public sealed record RemoveMemberCommand(
    Guid UserId,
    Guid HomeId,
    Guid MembershipId) : IRequest<Result<RemoveMemberResponse>>;