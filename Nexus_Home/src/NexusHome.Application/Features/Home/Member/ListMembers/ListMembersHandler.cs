namespace NexusHome.Application.Features.Home.Member.ListMembers;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for ListMembersQuery.
/// </summary>
public sealed class ListMembersHandler : IRequestHandler<ListMembersQuery, Result<ListMembersResponse>>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public ListMembersHandler(
        IMembershipRepository membershipRepository,
        IHomeRepository homeRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _membershipRepository = membershipRepository;
        _homeRepository = homeRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<ListMembersResponse>> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<ListMembersResponse>.Failure(homeResult.Error);
        }

        var home = homeResult.Value!;

        // Get members with cursor-based pagination
        var members = await _membershipRepository.ListAsync(
            request.HomeId,
            request.Cursor,
            request.Limit,
            cancellationToken);

        var memberItems = members.Select(m => new MemberListItem(
            m.Id,
            m.UserId,
            m.User?.Email ?? string.Empty,
            m.User?.FullName ?? string.Empty,
            m.Role.ToString(),
            m.IsActive,
            m.JoinedAt)).ToList();

        // Generate next cursor if we have more results
        string? nextCursor = null;
        if (members.Count == request.Limit)
        {
            var lastMember = members.Last();
            nextCursor = GenerateCursor(lastMember.CreatedAt, lastMember.Id);
        }

        var response = new ListMembersResponse(memberItems, nextCursor);
        return Result<ListMembersResponse>.Success(response);
    }

    private static string GenerateCursor(DateTimeOffset createdAt, Guid id)
    {
        var cursorString = $"{createdAt:O}|{id}";
        var cursorBytes = System.Text.Encoding.UTF8.GetBytes(cursorString);
        return Convert.ToBase64String(cursorBytes);
    }
}