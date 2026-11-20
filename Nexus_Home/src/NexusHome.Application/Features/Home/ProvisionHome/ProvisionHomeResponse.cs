namespace NexusHome.Application.Features.Home.ProvisionHome;

/// <summary>
/// Response for ProvisionHomeCommand.
/// </summary>
public sealed record ProvisionHomeResponse(
    HomeResponse Home);

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