namespace NexusHome.Infrastructure.Authentication;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// JWT token service implementation.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ClaimCredentialSettings _claimCredentialSettings;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        IOptions<ClaimCredentialSettings> claimCredentialSettings,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _jwtSettings = jwtSettings.Value;
        _claimCredentialSettings = claimCredentialSettings.Value;
        _refreshTokenRepository = refreshTokenRepository;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email, user.Email),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
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
    public (string PlainCredential, string CredentialHash) GenerateClaimCredential()
    {
        // Generate a cryptographically secure random credential (configured bytes, default 32 = 256 bits)
        var randomBytes = RandomNumberGenerator.GetBytes(_claimCredentialSettings.TokenBytes);

        // Encode as Base64URL for safe transmission
        var plainCredential = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Hash the credential with SHA-256 for storage
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(randomBytes);
        var credentialHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (plainCredential, credentialHash);
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
    public int AccessTokenExpirationSeconds => _jwtSettings.AccessTokenExpirationMinutes * 60;

    /// <inheritdoc />
    public int RefreshTokenExpirationDays => _jwtSettings.RefreshTokenExpirationDays;

    /// <inheritdoc />
    public int ClaimCredentialExpirationDays => _claimCredentialSettings.ExpiryDays;
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