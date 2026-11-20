namespace NexusHome.Application.Features.Home.RevokeClaimCredential;

using FluentValidation;

/// <summary>
/// Validator for RevokeClaimCredentialCommand.
/// </summary>
public sealed class RevokeClaimCredentialCommandValidator : AbstractValidator<RevokeClaimCredentialCommand>
{
    public RevokeClaimCredentialCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("Home ID is required");
    }
}