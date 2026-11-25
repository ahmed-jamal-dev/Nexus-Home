namespace NexusHome.Application.Features.Home.Member.AcceptInvitation;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to accept a member invitation.
/// </summary>
public sealed record AcceptInvitationCommand(
    Guid UserId,
    string Token) : IRequest<Result<AcceptInvitationResponse>>;