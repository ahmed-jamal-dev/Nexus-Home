namespace NexusHome.Application.Features.Authentication.Login;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to log in a user.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<LoginResponse>>;