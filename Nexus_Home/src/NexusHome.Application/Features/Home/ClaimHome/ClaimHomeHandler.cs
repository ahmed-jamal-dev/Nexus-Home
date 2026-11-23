namespace NexusHome.Application.Features.Home.ClaimHome;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Domain.Events;
using System.Security.Cryptography;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for ClaimHomeCommand.
/// </summary>
public sealed class ClaimHomeHandler : IRequestHandler<ClaimHomeCommand, Result<ClaimHomeResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IClaimCredentialRepository _claimCredentialRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public ClaimHomeHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository,
        IUserRepository userRepository,
        IMembershipRepository membershipRepository,
        IOutboxEventRepository outboxEventRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<ClaimHomeResponse>> Handle(ClaimHomeCommand request, CancellationToken cancellationToken)
    {
        // Hash the provided credential
        var credentialHash = HashCredential(request.Credential);

        // Find the claim credential by hash
        var credential = await _claimCredentialRepository.GetByTokenHashAsync(credentialHash, cancellationToken);
        if (credential == null)
        {
            return Errors.Home.ClaimCredentialNotFound;
        }

        // Acquire row lock on the credential to prevent concurrent claim attempts
        // We use the home ID from the credential we already found
        var lockedCredential = await _claimCredentialRepository.GetPendingForUpdateWithLockAsync(credential.HomeId, cancellationToken);
        if (lockedCredential == null || lockedCredential.Id != credential.Id)
        {
            // Credential was concurrently claimed or revoked
            return Errors.Home.ClaimCredentialAlreadyUsed;
        }
        // Keep the original tracked 'credential' entity for the update.
        // 'lockedCredential' was loaded with AsNoTracking and is a different instance with the same key.
        // Using it for UpdateAsync would cause "another instance with the same key value is already being tracked".

        // Validate credential state
        if (credential.IsAccepted)
        {
            return Errors.Home.ClaimCredentialAlreadyUsed;
        }

        if (credential.IsExpired)
        {
            return Errors.Home.ClaimCredentialExpired;
        }

        if (string.IsNullOrEmpty(credential.Token))
        {
            return Errors.Home.ClaimCredentialRevoked;
        }

        // Check if user already owns this home
        if (await _homeRepository.UserOwnsHomeAsync(request.UserId, credential.HomeId, cancellationToken))
        {
            return Errors.Home.UserAlreadyOwnsHome;
        }

        // Get the home to verify it exists and isn't deleted
        var homeEntity = await _homeRepository.GetByIdAsync(credential.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        // Get the user to verify they exist and are active
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Auth.UserNotFound;
        }

        // Atomic operation: Accept credential + Create Owner Membership
        // Both repositories use the same scoped DbContext, so this is atomic

        // Accept the credential
        credential.Accept();
        await _claimCredentialRepository.UpdateAsync(credential, cancellationToken);

        // Create Owner membership
        var membership = new Membership(
            Guid.NewGuid(),
            request.UserId,
            credential.HomeId,
            Role.Owner);

        await _membershipRepository.AddAsync(membership, cancellationToken);

        // Publish HomeClaimed outbox event
        var homeClaimedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "home.claimed",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = credential.HomeId,
                userId = request.UserId,
                membershipId = membership.Id,
                claimedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = credential.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(homeClaimedEvent, cancellationToken);

        // Build response
        var homeResponse = new HomeResponse(
            homeEntity.Id,
            homeEntity.Name,
            homeEntity.AddressLine1,
            homeEntity.AddressLine2,
            homeEntity.City,
            homeEntity.StateProvince,
            homeEntity.PostalCode,
            homeEntity.Timezone,
            homeEntity.CreatedAt,
            homeEntity.UpdatedAt);

        var membershipResponse = new MembershipResponse(
            membership.Id,
            membership.UserId,
            user.Email,
            user.FullName,
            membership.Role,
            membership.IsActive,
            membership.JoinedAt);

        return Result<ClaimHomeResponse>.Success(new ClaimHomeResponse(homeResponse, membershipResponse));
    }

    private static string HashCredential(string credential)
    {
        // Decode from Base64URL
        var base64 = credential
            .Replace("-", "+")
            .Replace("_", "/");

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var credentialBytes = Convert.FromBase64String(base64);

        // Hash with SHA-256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(credentialBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}