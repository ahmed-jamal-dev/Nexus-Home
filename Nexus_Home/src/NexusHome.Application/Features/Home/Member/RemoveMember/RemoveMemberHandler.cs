namespace NexusHome.Application.Features.Home.Member.RemoveMember;

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
/// Handler for RemoveMemberCommand.
/// </summary>
public sealed class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand, Result<RemoveMemberResponse>>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public RemoveMemberHandler(
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

    public async Task<Result<RemoveMemberResponse>> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (must be Owner)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<RemoveMemberResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Check if user is Owner of this home
        if (!await _homeRepository.UserOwnsHomeAsync(request.UserId, home.Id, cancellationToken))
        {
            return Result<RemoveMemberResponse>.Failure(Errors.Home.ProvisioningForbidden);
        }

        // Get the membership to remove
        var membership = await _membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken);
        if (membership == null)
        {
            return Result<RemoveMemberResponse>.Failure(Error.Create("MEMBER.NOT_FOUND", "Member not found"));
        }

        // Verify membership belongs to the same home
        if (membership.HomeId != request.HomeId)
        {
            return Result<RemoveMemberResponse>.Failure(Error.Create("MEMBER.NOT_FOUND", "Member not found in this home"));
        }

        // Acquire advisory lock for last-owner protection before check + modification
        var lockAcquired = await _membershipRepository.TryAcquireLastOwnerLockAsync(request.HomeId, cancellationToken);
        if (!lockAcquired)
        {
            return Result<RemoveMemberResponse>.Failure(Error.Create("MEMBER.CONCURRENCY_CONFLICT", "Another operation is modifying home ownership. Please retry."));
        }

        // Check if removing self and is last Owner
        if (membership.UserId == request.UserId)
        {
            var isLastOwner = await IsLastActiveOwnerAsync(request.HomeId, cancellationToken);
            if (isLastOwner)
            {
                return Result<RemoveMemberResponse>.Failure(Error.Create("MEMBER.CANNOT_REMOVE_LAST_OWNER", "Cannot remove the last active owner of the home"));
            }
        }

        // Cannot remove another Owner if they are the last active Owner
        if (membership.Role == Role.Owner)
        {
            var isLastOwner = await IsLastActiveOwnerAsync(request.HomeId, cancellationToken);
            if (isLastOwner)
            {
                return Result<RemoveMemberResponse>.Failure(Error.Create("MEMBER.CANNOT_REMOVE_LAST_OWNER", "Cannot remove the last active owner of the home"));
            }
        }

        // Deactivate the membership (soft delete)
        membership.Deactivate();
        await _membershipRepository.UpdateAsync(membership, cancellationToken);

        // Publish member.removed outbox event
        var memberRemovedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "member.removed",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                membershipId = membership.Id,
                removedUserId = membership.UserId,
                removedByUserId = request.UserId,
                removedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(memberRemovedEvent, cancellationToken);

        var response = new RemoveMemberResponse(
            membership.Id,
            membership.HomeId,
            membership.UserId);

        return Result<RemoveMemberResponse>.Success(response);
    }

    private async Task<bool> IsLastActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken)
    {
        var activeOwner = await _membershipRepository.GetActiveOwnerAsync(homeId, cancellationToken);
        return activeOwner != null && activeOwner.IsActive;
    }
}