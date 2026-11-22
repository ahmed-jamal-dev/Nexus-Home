namespace NexusHome.Application.Features.Home.GetClaimCredential;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Handler for GetClaimCredentialQuery.
/// </summary>
public sealed class GetClaimCredentialHandler : IRequestHandler<GetClaimCredentialQuery, Result<GetClaimCredentialResponse>>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IClaimCredentialRepository _claimCredentialRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public GetClaimCredentialHandler(
        IHomeRepository homeRepository,
        IClaimCredentialRepository claimCredentialRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _homeRepository = homeRepository;
        _claimCredentialRepository = claimCredentialRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<GetClaimCredentialResponse>> Handle(GetClaimCredentialQuery request, CancellationToken cancellationToken)
    {
        // Check home exists and is not deleted
        var homeEntity = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (homeEntity == null)
        {
            return Errors.Home.HomeNotFound;
        }

        // Verify technician scoping: technician must be provisionedByTechnicianId for this home
        // or have PlatformAdmin/SuperAdmin role
        var isPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.TechnicianUserId, cancellationToken);

        if (!isPlatformAdmin && homeEntity.ProvisionedByTechnicianId != request.TechnicianUserId)
        {
            return Errors.Home.ProvisioningForbidden;
        }

        var credential = await _claimCredentialRepository.GetPendingForUpdateAsync(request.HomeId, cancellationToken);
        if (credential == null)
        {
            return Errors.Home.ClaimCredentialNotFound;
        }

        var isRevoked = string.IsNullOrEmpty(credential.Token) || credential.ExpiresAt <= DateTimeOffset.UtcNow;

        return Result<GetClaimCredentialResponse>.Success(new GetClaimCredentialResponse(
            credential.ExpiresAt,
            credential.IsAccepted,
            isRevoked,
            request.HomeId));
    }
}