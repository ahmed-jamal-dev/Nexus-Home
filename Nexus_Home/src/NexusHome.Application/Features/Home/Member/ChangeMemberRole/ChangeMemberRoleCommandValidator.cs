namespace NexusHome.Application.Features.Home.Member.ChangeMemberRole;

using FluentValidation;

/// <summary>
/// Validator for ChangeMemberRoleCommand.
/// </summary>
public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == "Member" || role == "Guest")
            .WithMessage("Role must be 'Member' or 'Guest'");

        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.MembershipId)
            .NotEqual(Guid.Empty).WithMessage("Membership ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}