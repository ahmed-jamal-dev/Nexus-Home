namespace NexusHome.Application.Features.Home.Member.InviteMember;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for InviteMemberCommand.
/// </summary>
public sealed class InviteMemberHandler : IRequestHandler<InviteMemberCommand, Result<InviteMemberResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITokenService _tokenService;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public InviteMemberHandler(
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository invitationRepository,
        ITokenService tokenService,
        IOutboxEventRepository outboxEventRepository,
        IEmailService emailService,
        IUserRepository userRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _invitationRepository = invitationRepository;
        _tokenService = tokenService;
        _outboxEventRepository = outboxEventRepository;
        _emailService = emailService;
        _userRepository = userRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<InviteMemberResponse>> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (must be Owner)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<InviteMemberResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Check if user is Owner of this home
        if (!await _homeRepository.UserOwnsHomeAsync(request.UserId, home.Id, cancellationToken))
        {
            return Result<InviteMemberResponse>.Failure(Errors.Home.ProvisioningForbidden);
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.Role, true, out var invitedRole) ||
            (invitedRole != Role.Member && invitedRole != Role.Guest))
        {
            return Result<InviteMemberResponse>.Failure(Error.Create("VALIDATION.INVALID_ROLE", "Role must be 'Member' or 'Guest'"));
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // Check if email already has a pending invitation for this home
        var emailExists = await _invitationRepository.ExistsPendingForEmailAsync(request.HomeId, email, cancellationToken);
        if (emailExists)
        {
            return Result<InviteMemberResponse>.Failure(Error.Create("MEMBER.INVITATION_EXISTS", "A pending invitation already exists for this email"));
        }

        // Check if email is already an active member of this home
        var invitedUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (invitedUser != null)
        {
            var isActiveMember = await _membershipRepository.IsActiveMemberAsync(invitedUser.Id, request.HomeId, cancellationToken);
            if (isActiveMember)
            {
                return Result<InviteMemberResponse>.Failure(Error.Create("MEMBER.ALREADY_MEMBER", "This user is already a member of the home"));
            }
        }

        // Generate invitation token
        var (plainToken, tokenHash) = _tokenService.GenerateMemberInvitationToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_tokenService.ClaimCredentialExpirationDays);

        // Create invitation
        var invitation = Invitation.CreateMemberInvitation(
            request.HomeId,
            email,
            invitedRole,
            tokenHash,
            expiresAt,
            request.UserId);

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        // Publish member.invited outbox event
        var invitationCreatedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "member.invited",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                invitationId = invitation.Id,
                email = email,
                role = invitedRole.ToString(),
                expiresAt = expiresAt,
                createdAt = DateTimeOffset.UtcNow,
                invitedByUserId = request.UserId
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(invitationCreatedEvent, cancellationToken);

        // Send invitation email (mock)
        var inviter = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var inviterName = inviter?.FullName ?? "User";
        await _emailService.SendMemberInvitationAsync(
            email,
            plainToken,
            home.Name,
            invitedRole.ToString(),
            inviterName,
            expiresAt,
            cancellationToken);

        var response = new InviteMemberResponse(
            invitation.Id,
            email,
            invitedRole.ToString(),
            expiresAt,
            plainToken);

        return Result<InviteMemberResponse>.Success(response);
    }
}