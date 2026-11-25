namespace NexusHome.Application.Features.Home.Member.RemoveMember;

using FluentValidation;

/// <summary>
/// Validator for RemoveMemberCommand.
/// </summary>
public sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.MembershipId)
            .NotEqual(Guid.Empty).WithMessage("Membership ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}