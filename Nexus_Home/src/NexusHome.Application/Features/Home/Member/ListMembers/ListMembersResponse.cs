namespace NexusHome.Application.Features.Home.Member.ListMembers;

/// <summary>
/// Response for ListMembersQuery.
/// </summary>
public sealed record ListMembersResponse(
    List<MemberListItem> Members,
    string? NextCursor);

/// <summary>
/// Member list item.
/// </summary>
public sealed record MemberListItem(
    Guid MembershipId,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTimeOffset JoinedAt);