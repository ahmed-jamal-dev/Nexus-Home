namespace NexusHome.Application.Features.Home.ProvisionHome;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to provision a new home (internal/trusted operation).
/// </summary>
public sealed record ProvisionHomeCommand(
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? StateProvince = null,
    string? PostalCode = null,
    string? Timezone = null) : IRequest<Result<ProvisionHomeResponse>>;