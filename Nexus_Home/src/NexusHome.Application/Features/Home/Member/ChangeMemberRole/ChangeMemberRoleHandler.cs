namespace NexusHome.Application.Features.Home.Member.ChangeMemberRole;

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
/// Handler for ChangeMemberRoleCommand.
/// </summary>
public sealed class ChangeMemberRoleHandler : IRequestHandler<ChangeMemberRoleCommand, Result<ChangeMemberRoleResponse>>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public ChangeMemberRoleHandler(
        IMembershipRepository membershipRepository,
        IHomeRepository homeRepository,
        IOutboxEventRepository outboxEventRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _membershipRepository = membershipRepository;
        _homeRepository = homeRepository;
        _outboxEventRepository = outboxEventRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<ChangeMemberRoleResponse>> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (must be Owner)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<ChangeMemberRoleResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Check if user is Owner of this home
        if (!await _homeRepository.UserOwnsHomeAsync(request.UserId, home.Id, cancellationToken))
        {
            return Result<ChangeMemberRoleResponse>.Failure(Errors.Home.ProvisioningForbidden);
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.Role, true, out var newRole) ||
            (newRole != Role.Member && newRole != Role.Guest))
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("VALIDATION.INVALID_ROLE", "Role must be 'Member' or 'Guest'"));
        }

        // Get the membership to change
        var membership = await _membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken);
        if (membership == null)
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.NOT_FOUND", "Member not found"));
        }

        // Verify membership belongs to the same home
        if (membership.HomeId != request.HomeId)
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.NOT_FOUND", "Member not found in this home"));
        }

        // Cannot change Owner role
        if (membership.Role == Role.Owner)
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.CANNOT_CHANGE_OWNER_ROLE", "Cannot change the role of an Owner"));
        }

        // Acquire advisory lock for last-owner protection before check + modification
        var lockAcquired = await _membershipRepository.TryAcquireLastOwnerLockAsync(request.HomeId, cancellationToken);
        if (!lockAcquired)
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.CONCURRENCY_CONFLICT", "Another operation is modifying home ownership. Please retry."));
        }

        // Cannot change own role to non-Owner if last Owner
        if (membership.UserId == request.UserId)
        {
            var isLastOwner = await IsLastActiveOwnerAsync(request.HomeId, cancellationToken);
            if (isLastOwner)
            {
                return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.CANNOT_DEMOTE_LAST_OWNER", "Cannot change your role as the last active owner"));
            }
        }

        // If trying to change to Owner, block it (already validated above that newRole is Member or Guest)
        // This is an additional safeguard
        if (newRole == Role.Owner)
        {
            return Result<ChangeMemberRoleResponse>.Failure(Error.Create("MEMBER.CANNOT_ASSIGN_OWNER_ROLE", "Cannot assign Owner role through member management"));
        }

        // Change the role
        var oldRole = membership.Role;
        membership.ChangeRole(newRole);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);

        // Publish member.role_changed outbox event
        var memberRoleChangedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "member.role.changed",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                membershipId = membership.Id,
                userId = membership.UserId,
                oldRole = oldRole.ToString(),
                newRole = newRole.ToString(),
                changedByUserId = request.UserId,
                changedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(memberRoleChangedEvent, cancellationToken);

        var response = new ChangeMemberRoleResponse(
            membership.Id,
            membership.HomeId,
            membership.UserId,
            membership.Role.ToString());

        return Result<ChangeMemberRoleResponse>.Success(response);
    }

    private async Task<bool> IsLastActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken)
    {
        var activeOwner = await _membershipRepository.GetActiveOwnerAsync(homeId, cancellationToken);
        return activeOwner != null && activeOwner.IsActive;
    }
}