namespace NexusHome.Application.Features.Home.Member.InviteMember;

using FluentValidation;
using NexusHome.Domain.Enums;

/// <summary>
/// Validator for InviteMemberCommand.
/// </summary>
public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == "Member" || role == "Guest")
            .WithMessage("Role must be 'Member' or 'Guest'");

        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}