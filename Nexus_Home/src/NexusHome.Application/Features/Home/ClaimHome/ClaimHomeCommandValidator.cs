namespace NexusHome.Application.Features.Home.ClaimHome;

using FluentValidation;

/// <summary>
/// Validator for ClaimHomeCommand.
/// </summary>
public sealed class ClaimHomeCommandValidator : AbstractValidator<ClaimHomeCommand>
{
    public ClaimHomeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Credential)
            .NotEmpty().WithMessage("Claim credential is required")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Invalid claim credential format");
    }
}