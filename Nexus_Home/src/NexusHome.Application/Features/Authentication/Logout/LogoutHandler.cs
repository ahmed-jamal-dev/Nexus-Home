namespace NexusHome.Application.Features.Authentication.Logout;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for LogoutCommand.
/// </summary>
public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Hash the provided refresh token
        string tokenHash;
        try
        {
            tokenHash = HashToken(request.RefreshToken);
        }
        catch (FormatException)
        {
            return Auth.InvalidRefreshToken;
        }

        // Find and revoke the token
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token != null && token.RevokedAt == null)
        {
            token.Revoke("User logout");
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }

        // Always return success for logout (idempotent)
        return Result<LogoutResponse>.Success(new LogoutResponse(true));
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var tokenBytes = Convert.FromBase64String(token);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}