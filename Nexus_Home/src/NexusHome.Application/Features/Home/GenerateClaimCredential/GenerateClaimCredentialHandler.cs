namespace NexusHome.Application.Features.Home.GenerateClaimCredential;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for GenerateClaimCredentialCommand.
/// </summary>
public sealed class GenerateClaimCredentialHandler : IRequestHandler<GenerateClaimCredentialCommand, Result<GenerateClaimCredentialResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IClaimCredentialRepository _claimCredentialRepository;
    private readonly ITokenService _tokenService;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public GenerateClaimCredentialHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository,
        ITokenService tokenService,
        IOutboxEventRepository outboxEventRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
        _tokenService = tokenService;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<GenerateClaimCredentialResponse>> Handle(GenerateClaimCredentialCommand request, CancellationToken cancellationToken)
    {
        // Check home exists and is not deleted
        var homeEntity = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        // Check if home already has an active owner
        if (await _homeRepository.HasActiveOwnerAsync(request.HomeId, cancellationToken))
        {
            return Errors.Home.HomeAlreadyClaimed;
        }

        // Check if a valid claim credential already exists
        if (await _claimCredentialRepository.ExistsValidCredentialAsync(request.HomeId, cancellationToken))
        {
            // Return existing credential info (without plaintext)
            var existing = await _claimCredentialRepository.GetPendingForUpdateAsync(request.HomeId, cancellationToken);
            if (existing != null && existing.IsValid)
            {
                return Result<GenerateClaimCredentialResponse>.Success(new GenerateClaimCredentialResponse(
                    Credential: "***EXISTING_CREDENTIAL***", // Plaintext not returned again
                    ExpiresAt: existing.ExpiresAt,
                    HomeId: request.HomeId));
            }
        }

        // Generate new credential
        var (plainCredential, credentialHash) = _tokenService.GenerateClaimCredential();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_tokenService.ClaimCredentialExpirationDays);

        var claimCredential = Invitation.CreateClaimCredential(
            request.HomeId,
            credentialHash,
            expiresAt);

        await _claimCredentialRepository.AddAsync(claimCredential, cancellationToken);

        // Publish ClaimCredentialGenerated outbox event
        var credentialGeneratedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "claim.credential.generated",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                credentialId = claimCredential.Id,
                expiresAt = expiresAt,
                generatedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(credentialGeneratedEvent, cancellationToken);

        return Result<GenerateClaimCredentialResponse>.Success(new GenerateClaimCredentialResponse(
            plainCredential,
            expiresAt,
            request.HomeId));
    }
}