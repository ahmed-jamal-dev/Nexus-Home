namespace NexusHome.Infrastructure.Authentication;

/// <summary>
/// Settings for claim credential generation and validation.
/// </summary>
public sealed class ClaimCredentialSettings
{
    /// <summary>
    /// Number of days until claim credential expires. Default: 7.
    /// </summary>
    public int ExpiryDays { get; init; } = 7;

    /// <summary>
    /// Number of random bytes for credential generation. Default: 32 (256 bits).
    /// </summary>
    public int TokenBytes { get; init; } = 32;
}