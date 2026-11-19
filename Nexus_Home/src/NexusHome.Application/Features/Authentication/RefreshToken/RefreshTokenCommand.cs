namespace NexusHome.Application.Features.Authentication.RefreshToken;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to refresh the access token.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken
) : IRequest<Result<RefreshTokenResponse>>;