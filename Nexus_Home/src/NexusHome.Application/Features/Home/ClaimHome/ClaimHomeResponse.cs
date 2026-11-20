namespace NexusHome.Application.Features.Home.ClaimHome;

using NexusHome.Domain.Enums;

/// <summary>
/// Response for ClaimHomeCommand.
/// </summary>
public sealed record ClaimHomeResponse(
    HomeResponse Home,
    MembershipResponse Membership);

/// <summary>
/// Home data transfer object.
/// </summary>
public sealed record HomeResponse(
    Guid Id,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string Timezone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Membership data transfer object.
/// </summary>
public sealed record MembershipResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    Role Role,
    bool IsActive,
    DateTimeOffset JoinedAt);