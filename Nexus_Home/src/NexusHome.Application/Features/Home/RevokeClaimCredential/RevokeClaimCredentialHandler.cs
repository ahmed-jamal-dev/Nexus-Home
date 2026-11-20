namespace NexusHome.Application.Features.Home.RevokeClaimCredential;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for RevokeClaimCredentialCommand.
/// </summary>
public sealed class RevokeClaimCredentialHandler : IRequestHandler<RevokeClaimCredentialCommand, Result>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IClaimCredentialRepository _claimCredentialRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public RevokeClaimCredentialHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository,
        IOutboxEventRepository outboxEventRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result> Handle(RevokeClaimCredentialCommand request, CancellationToken cancellationToken)
    {
        // Check home exists and is not deleted
        var homeEntity = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        var credential = await _claimCredentialRepository.GetPendingForUpdateAsync(request.HomeId, cancellationToken);
        if (credential == null)
        {
            return Errors.Home.ClaimCredentialNotFound;
        }

        var credentialId = credential.Id;

        // Revoke the credential
        credential.Revoke();
        await _claimCredentialRepository.UpdateAsync(credential, cancellationToken);

        // Publish ClaimCredentialRevoked outbox event
        var credentialRevokedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "claim.credential.revoked",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                credentialId = credentialId,
                revokedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(credentialRevokedEvent, cancellationToken);

        return Result.Success();
    }
}