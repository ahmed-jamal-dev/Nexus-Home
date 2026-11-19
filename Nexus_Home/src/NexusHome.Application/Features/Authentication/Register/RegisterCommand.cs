namespace NexusHome.Application.Features.Authentication.Register;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to register a new user.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName
) : IRequest<Result<RegisterResponse>>;