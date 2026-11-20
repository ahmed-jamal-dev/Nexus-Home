namespace NexusHome.Application.Interfaces;

using NexusHome.Application.Common;
using NexusHome.Domain.Entities;

/// <summary>
/// Service for JWT token operations.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for the user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token and returns the plaintext token and its hash.
    /// </summary>
    (string PlainToken, string TokenHash) GenerateRefreshToken();

    /// <summary>
    /// Generates a claim credential and returns the plaintext credential and its hash.
    /// </summary>
    (string PlainCredential, string CredentialHash) GenerateClaimCredential();

    /// <summary>
    /// Validates a refresh token hash and returns the associated user if valid.
    /// </summary>
    Task<Result<User>> ValidateRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the access token expiration time in seconds.
    /// </summary>
    int AccessTokenExpirationSeconds { get; }

    /// <summary>
    /// Gets the refresh token expiration time in days.
    /// </summary>
    int RefreshTokenExpirationDays { get; }

    /// <summary>
    /// Gets the claim credential expiration time in days.
    /// </summary>
    int ClaimCredentialExpirationDays { get; }
}