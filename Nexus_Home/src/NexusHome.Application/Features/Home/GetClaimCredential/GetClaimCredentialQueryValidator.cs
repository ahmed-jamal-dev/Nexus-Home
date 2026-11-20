namespace NexusHome.Application.Features.Home.GetClaimCredential;

using FluentValidation;

/// <summary>
/// Validator for GetClaimCredentialQuery.
/// </summary>
public sealed class GetClaimCredentialQueryValidator : AbstractValidator<GetClaimCredentialQuery>
{
    public GetClaimCredentialQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("Home ID is required");
    }
}