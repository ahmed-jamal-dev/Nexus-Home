namespace NexusHome.Application.Features.Home.ProvisionHome;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for ProvisionHomeCommand.
/// </summary>
public sealed class ProvisionHomeHandler : IRequestHandler<ProvisionHomeCommand, Result<ProvisionHomeResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public ProvisionHomeHandler(
        IHomeRepository homeRepository,
        IOutboxEventRepository outboxEventRepository)
    {
        _homeRepository = homeRepository;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<ProvisionHomeResponse>> Handle(ProvisionHomeCommand request, CancellationToken cancellationToken)
    {
        var homeEntity = new DomainHome(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Timezone?.Trim() ?? "UTC");

        if (!string.IsNullOrWhiteSpace(request.AddressLine1))
            homeEntity.UpdateDetails(
                homeEntity.Name,
                request.AddressLine1.Trim(),
                request.AddressLine2?.Trim(),
                request.City?.Trim(),
                request.StateProvince?.Trim(),
                request.PostalCode?.Trim(),
                request.Timezone?.Trim());

        await _homeRepository.AddAsync(homeEntity, cancellationToken);

        // Publish HomeProvisioned outbox event
        var homeProvisionedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "home.provisioned",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = homeEntity.Id,
                name = homeEntity.Name,
                timezone = homeEntity.Timezone,
                addressLine1 = homeEntity.AddressLine1,
                addressLine2 = homeEntity.AddressLine2,
                city = homeEntity.City,
                stateProvince = homeEntity.StateProvince,
                postalCode = homeEntity.PostalCode,
                provisionedAt = homeEntity.CreatedAt
            }),
            CorrelationId = homeEntity.Id.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(homeProvisionedEvent, cancellationToken);

        var response = new HomeResponse(
            homeEntity.Id,
            homeEntity.Name,
            homeEntity.AddressLine1,
            homeEntity.AddressLine2,
            homeEntity.City,
            homeEntity.StateProvince,
            homeEntity.PostalCode,
            homeEntity.Timezone,
            homeEntity.CreatedAt,
            homeEntity.UpdatedAt);

        return Result<ProvisionHomeResponse>.Success(new ProvisionHomeResponse(response));
    }
}