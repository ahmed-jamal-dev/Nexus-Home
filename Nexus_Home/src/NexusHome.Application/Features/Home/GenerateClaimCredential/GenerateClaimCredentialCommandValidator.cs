namespace NexusHome.Application.Features.Home.GenerateClaimCredential;

using FluentValidation;

/// <summary>
/// Validator for GenerateClaimCredentialCommand.
/// </summary>
public sealed class GenerateClaimCredentialCommandValidator : AbstractValidator<GenerateClaimCredentialCommand>
{
    public GenerateClaimCredentialCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("Home ID is required");
    }
}