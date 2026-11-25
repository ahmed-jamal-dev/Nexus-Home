namespace NexusHome.Application.Features.Home.Member.AcceptInvitation;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Domain.Events;
using System.Security.Cryptography;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for AcceptInvitationCommand.
/// </summary>
public sealed class AcceptInvitationHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public AcceptInvitationHandler(
        IInvitationRepository invitationRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        IOutboxEventRepository outboxEventRepository)
    {
        _invitationRepository = invitationRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<AcceptInvitationResponse>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        // Hash the provided token
        var tokenHash = HashToken(request.Token);

        // Find the invitation by hash
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invitation == null)
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVITATION_NOT_FOUND", "Invalid or expired invitation"));
        }

        // Acquire row lock on the invitation to prevent concurrent acceptance
        var lockedInvitation = await _invitationRepository.GetByTokenHashWithLockAsync(tokenHash, cancellationToken);
        if (lockedInvitation == null || lockedInvitation.Id != invitation.Id)
        {
            // Invitation was concurrently accepted or revoked
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVITATION_ALREADY_USED", "Invitation has already been used"));
        }
        // Keep the original tracked 'invitation' entity for the update.
        // 'lockedInvitation' was loaded with AsNoTracking and is a different instance with the same key.
        // Using it for UpdateAsync would cause "another instance with the same key value is already being tracked".

        // Validate invitation state
        if (invitation.IsAccepted)
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVITATION_ALREADY_USED", "Invitation has already been accepted"));
        }

        if (invitation.IsExpired)
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVITATION_EXPIRED", "Invitation has expired"));
        }

        if (string.IsNullOrEmpty(invitation.Token))
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVITATION_REVOKED", "Invitation has been revoked"));
        }

        // Verify this is a member invitation (not a claim credential)
        if (invitation.IsClaimCredential)
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.INVALID_INVITATION_TYPE", "This is a claim credential, not a member invitation"));
        }

        // Get the user to verify they exist and are active
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Result<AcceptInvitationResponse>.Failure(Errors.Auth.UserNotFound);
        }

        // Verify the email matches the invitation
        if (!string.Equals(user.Email, invitation.InviteeEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.EMAIL_MISMATCH", "Invitation email does not match your account"));
        }

        // Check if user is already a member of this home
        var isActiveMember = await _membershipRepository.IsActiveMemberAsync(request.UserId, invitation.HomeId, cancellationToken);
        if (isActiveMember)
        {
            return Result<AcceptInvitationResponse>.Failure(Error.Create("MEMBER.ALREADY_MEMBER", "You are already a member of this home"));
        }

        // Atomic operation: Accept invitation + Create Membership
        // Both repositories use the same scoped DbContext, so this is atomic

        // Accept the invitation
        invitation.Accept();
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        // Create Membership with the invited role
        var membership = new Membership(
            Guid.NewGuid(),
            request.UserId,
            invitation.HomeId,
            invitation.Role);

        await _membershipRepository.AddAsync(membership, cancellationToken);

        // Publish invitation.accepted outbox event
        var invitationAcceptedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "invitation.accepted",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = invitation.HomeId,
                invitationId = invitation.Id,
                userId = request.UserId,
                membershipId = membership.Id,
                role = invitation.Role.ToString(),
                acceptedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = invitation.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(invitationAcceptedEvent, cancellationToken);

        var response = new AcceptInvitationResponse(
            membership.Id,
            membership.HomeId,
            membership.Role.ToString(),
            membership.JoinedAt);

        return Result<AcceptInvitationResponse>.Success(response);
    }

    private static string HashToken(string token)
    {
        // Decode from Base64URL
        var base64 = token
            .Replace("-", "+")
            .Replace("_", "/");

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var tokenBytes = Convert.FromBase64String(base64);

        // Hash with SHA-256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}