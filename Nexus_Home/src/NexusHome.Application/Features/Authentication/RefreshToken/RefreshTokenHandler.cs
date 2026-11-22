namespace NexusHome.Application.Features.Authentication.RefreshToken;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
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

        // Find and validate refresh token
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken == null)
        {
            return Auth.InvalidRefreshToken;
        }

        if (refreshToken.RevokedAt != null)
        {
            return Auth.RefreshTokenRevoked;
        }

        if (refreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Auth.RefreshTokenExpired;
        }

        // Get the user
        var user = refreshToken.User;
        if (user == null || !user.IsActive)
        {
            return Auth.UserInactive;
        }

        // Rotate the token: revoke old, create new
        var (newPlainToken, newTokenHash) = _tokenService.GenerateRefreshToken();
        refreshToken.Revoke("Token rotated", newTokenHash);

        var newRefreshToken = new RefreshToken(
            Guid.NewGuid(),
            newTokenHash,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays)
        );

        // Save both tokens atomically - update old, add new
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // Get user's platform roles
        var userPlatformRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(user.Id, cancellationToken);
        var activeRoles = userPlatformRoles.Where(upr => upr.IsValid()).ToList();

        var roleKeys = activeRoles.Select(upr => upr.PlatformRole.Key).Distinct().ToList();
        var roleScopes = activeRoles
            .Select(upr => (upr.PlatformRole.Key, upr.ScopeId, upr.ScopeType))
            .ToList();

        // Generate new access token with current roles
        var accessToken = _tokenService.GenerateAccessToken(user, roleKeys, roleScopes, user.PlatformRolesVersion);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            new TokenResponse(
                accessToken,
                newPlainToken,
                _tokenService.AccessTokenExpirationSeconds
            )
        ));
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var tokenBytes = Convert.FromBase64String(token);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}