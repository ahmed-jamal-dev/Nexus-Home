namespace NexusHome.Application.Features.Home.GetClaimCredential;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to retrieve claim credential metadata (internal/trusted operation).
/// Never returns the plaintext credential.
/// </summary>
public sealed record GetClaimCredentialQuery(
    Guid HomeId) : IRequest<Result<GetClaimCredentialResponse>>;