namespace NexusHome.Application.Features.Home.GenerateClaimCredential;

/// <summary>
/// Response for GenerateClaimCredentialCommand.
/// Contains the plaintext credential (only returned once at generation).
/// </summary>
public sealed record GenerateClaimCredentialResponse(
    string Credential,
    DateTimeOffset ExpiresAt,
    Guid HomeId);