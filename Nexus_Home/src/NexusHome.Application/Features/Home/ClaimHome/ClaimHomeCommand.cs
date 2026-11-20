namespace NexusHome.Application.Features.Home.ClaimHome;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to claim a home using a claim credential (authenticated user operation).
/// </summary>
public sealed record ClaimHomeCommand(
    Guid UserId,
    string Credential) : IRequest<Result<ClaimHomeResponse>>;