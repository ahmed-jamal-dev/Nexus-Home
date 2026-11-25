namespace NexusHome.Application.Features.Home.Member.AcceptInvitation;

using FluentValidation;

/// <summary>
/// Validator for AcceptInvitationCommand.
/// </summary>
public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Invitation token is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}