namespace NexusHome.Application.Features.Home.GetClaimCredential;

/// <summary>
/// Response for GetClaimCredentialQuery.
/// Contains only metadata - NEVER the plaintext credential.
/// </summary>
public sealed record GetClaimCredentialResponse(
    DateTimeOffset ExpiresAt,
    bool IsAccepted,
    bool IsRevoked,
    Guid HomeId);