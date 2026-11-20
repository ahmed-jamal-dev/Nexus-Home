namespace NexusHome.Application.Features.Home.GenerateClaimCredential;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to generate a claim credential for a home (internal/trusted operation).
/// </summary>
public sealed record GenerateClaimCredentialCommand(
    Guid HomeId) : IRequest<Result<GenerateClaimCredentialResponse>>;