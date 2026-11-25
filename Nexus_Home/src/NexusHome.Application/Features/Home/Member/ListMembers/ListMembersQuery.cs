namespace NexusHome.Application.Features.Home.Member.ListMembers;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list members of a home.
/// </summary>
public sealed record ListMembersQuery(
    Guid UserId,
    Guid HomeId,
    string? Cursor,
    int Limit) : IRequest<Result<ListMembersResponse>>;