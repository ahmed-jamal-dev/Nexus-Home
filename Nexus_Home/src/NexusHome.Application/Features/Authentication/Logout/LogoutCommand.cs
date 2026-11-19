namespace NexusHome.Application.Features.Authentication.Logout;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to log out a user.
/// </summary>
public sealed record LogoutCommand(
    string RefreshToken
) : IRequest<Result<LogoutResponse>>;