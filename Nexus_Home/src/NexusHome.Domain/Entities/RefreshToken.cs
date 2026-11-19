namespace NexusHome.Domain.Entities;

/// <summary>
/// Refresh token for JWT token rotation.
/// Stored as hash only - never plaintext.
/// </summary>
public class RefreshToken : Entity
{
    /// <summary>
    /// Hashed refresh token (SHA-256).
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// User ID this token belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// When the token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// When the token was revoked (if revoked).
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Token that replaced this one (for rotation tracking).
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Navigation to user.
    /// </summary>
    public User? User { get; private set; }

    protected RefreshToken() { }

    public RefreshToken(Guid id, string tokenHash, Guid userId, DateTimeOffset expiresAt)
        : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Checks if the token is valid (not expired, not revoked).
    /// </summary>
    public bool IsValid => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;

    /// <summary>
    /// Revokes the token.
    /// </summary>
    public void Revoke(string? reason = null, string? replacedByTokenHash = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
        Touch();
    }
}