namespace NexusHome.Application.Features.Authentication.GetCurrentUser;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get the current authenticated user.
/// </summary>
public sealed record GetCurrentUserQuery(
    Guid UserId
) : IRequest<Result<GetCurrentUserResponse>>;