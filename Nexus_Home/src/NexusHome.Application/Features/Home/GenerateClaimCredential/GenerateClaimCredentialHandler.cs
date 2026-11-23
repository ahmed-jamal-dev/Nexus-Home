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
    private readonly IPlatformAuditLogRepository _auditLogRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public GenerateClaimCredentialHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository,
        ITokenService tokenService,
        IOutboxEventRepository outboxEventRepository,
        IPlatformAuditLogRepository auditLogRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
        _tokenService = tokenService;
        _outboxEventRepository = outboxEventRepository;
        _auditLogRepository = auditLogRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<GenerateClaimCredentialResponse>> Handle(GenerateClaimCredentialCommand request, CancellationToken cancellationToken)
    {
        // Check home exists and is not deleted
        var homeEntity = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        // Verify technician scoping: technician must be provisionedByTechnicianId for this home
        // or have PlatformAdmin/SuperAdmin role
        var isPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.TechnicianUserId, cancellationToken);

        if (!isPlatformAdmin && homeEntity.ProvisionedByTechnicianId != request.TechnicianUserId)
        {
            return Errors.Home.ProvisioningForbidden;
        }

        // Check if home already has an active owner
        if (await _homeRepository.HasActiveOwnerAsync(request.HomeId, cancellationToken))
        {
            return Errors.Home.HomeAlreadyClaimed;
        }

        // Acquire advisory lock to prevent concurrent credential generation for the same home
        // This protects the check-then-insert race condition when no credential exists yet
        var lockAcquired = await _claimCredentialRepository.TryAcquireGenerationLockAsync(request.HomeId, cancellationToken);
        if (!lockAcquired)
        {
            return Result<GenerateClaimCredentialResponse>.Failure(Errors.Home.ClaimCredentialExists);
        }

        try
        {
            // Check if a valid claim credential already exists (with row lock)
            var existing = await _claimCredentialRepository.GetPendingForUpdateWithLockAsync(request.HomeId, cancellationToken);

            if (existing != null && existing.IsValid)
            {
                // Valid credential exists - return it (plaintext not returned again)
                return Result<GenerateClaimCredentialResponse>.Success(new GenerateClaimCredentialResponse(
                    Credential: "***EXISTING_CREDENTIAL***",
                    ExpiresAt: existing.ExpiresAt,
                    HomeId: request.HomeId));
            }

            // Generate new credential
            var (plainCredential, credentialHash) = _tokenService.GenerateClaimCredential();
            var expiresAt = DateTimeOffset.UtcNow.AddDays(_tokenService.ClaimCredentialExpirationDays);

            var claimCredential = Invitation.CreateClaimCredential(
                request.HomeId,
                credentialHash,
                expiresAt,
                request.TechnicianUserId);

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
                    generatedAt = DateTimeOffset.UtcNow,
                    generatedByTechnicianUserId = request.TechnicianUserId
                }),
                CorrelationId = request.HomeId.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _outboxEventRepository.AddAsync(credentialGeneratedEvent, cancellationToken);

            // Audit log
            var auditLog = PlatformAuditLog.Create(
                request.TechnicianUserId,
                "ClaimCredentialGenerated",
                "Home",
                request.HomeId,
                $"{{\"homeId\":\"{request.HomeId}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            return Result<GenerateClaimCredentialResponse>.Success(new GenerateClaimCredentialResponse(
                plainCredential,
                expiresAt,
                request.HomeId));
        }
        finally
        {
            await _claimCredentialRepository.ReleaseGenerationLockAsync(request.HomeId, cancellationToken);
        }
    }
}