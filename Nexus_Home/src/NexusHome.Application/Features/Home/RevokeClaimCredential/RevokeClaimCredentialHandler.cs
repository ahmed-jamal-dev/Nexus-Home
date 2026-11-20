namespace NexusHome.Application.Features.Home.RevokeClaimCredential;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for RevokeClaimCredentialCommand.
/// </summary>
public sealed class RevokeClaimCredentialHandler : IRequestHandler<RevokeClaimCredentialCommand, Result>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IClaimCredentialRepository _claimCredentialRepository;

    public RevokeClaimCredentialHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
    }

    public async Task<Result> Handle(RevokeClaimCredentialCommand request, CancellationToken cancellationToken)
    {
        // Check home exists and is not deleted
        var homeEntity = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        var credential = await _claimCredentialRepository.GetPendingForUpdateAsync(request.HomeId, cancellationToken);
        if (credential == null)
        {
            return Errors.Home.ClaimCredentialNotFound;
        }

        // Revoke the credential
        credential.Revoke();
        await _claimCredentialRepository.UpdateAsync(credential, cancellationToken);

        return Result.Success();
    }
}