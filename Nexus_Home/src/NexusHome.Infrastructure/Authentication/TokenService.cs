namespace NexusHome.Infrastructure.Authentication;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// JWT token service implementation.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenService(IOptions<JwtSettings> settings, IRefreshTokenRepository refreshTokenRepository)
    {
        _settings = settings.Value;
        _refreshTokenRepository = refreshTokenRepository;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public (string PlainToken, string TokenHash) GenerateRefreshToken()
    {
        // Generate a cryptographically secure random token (32 bytes = 256 bits)
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToBase64String(randomBytes);

        // Hash the token with SHA-256 for storage
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(randomBytes);
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (plainToken, tokenHash);
    }

    /// <inheritdoc />
    public async Task<Result<User>> ValidateRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken == null || !refreshToken.IsValid)
        {
            return Result<User>.Failure(Errors.Auth.InvalidRefreshToken);
        }

        return Result<User>.Success(refreshToken.User!);
    }

    /// <inheritdoc />
    public int AccessTokenExpirationSeconds => _settings.AccessTokenExpirationMinutes * 60;

    /// <inheritdoc />
    public int RefreshTokenExpirationDays => _settings.RefreshTokenExpirationDays;
}

/// <summary>
/// JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 15;
    public int RefreshTokenExpirationDays { get; init; } = 30;
    public string SigningKey { get; init; } = string.Empty;
}