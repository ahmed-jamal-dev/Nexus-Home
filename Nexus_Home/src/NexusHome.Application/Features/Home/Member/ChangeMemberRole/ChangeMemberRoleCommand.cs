namespace NexusHome.Application.Features.Home.Member.ChangeMemberRole;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to change a member's role in a home.
/// </summary>
public sealed record ChangeMemberRoleCommand(
    Guid UserId,
    Guid HomeId,
    Guid MembershipId,
    string Role) : IRequest<Result<ChangeMemberRoleResponse>>;