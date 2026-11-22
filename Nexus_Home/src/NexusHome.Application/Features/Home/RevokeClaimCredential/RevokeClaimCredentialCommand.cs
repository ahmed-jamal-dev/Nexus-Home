namespace NexusHome.Application.Features.Home.RevokeClaimCredential;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to revoke a claim credential (internal/trusted operation).
/// </summary>
public sealed record RevokeClaimCredentialCommand(
    Guid TechnicianUserId,
    Guid HomeId) : IRequest<Result>;