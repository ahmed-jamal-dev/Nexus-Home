namespace NexusHome.Application.Features.Home.Member.InviteMember;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to invite a member to a home.
/// </summary>
public sealed record InviteMemberCommand(
    Guid UserId,
    Guid HomeId,
    string Email,
    string Role) : IRequest<Result<InviteMemberResponse>>;