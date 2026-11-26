namespace NexusHome.Application.Features.Home.Permission.RevokePermission;

using FluentValidation;

/// <summary>
/// Validator for RevokePermissionCommand.
/// </summary>
public sealed class RevokePermissionCommandValidator : AbstractValidator<RevokePermissionCommand>
{
    public RevokePermissionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("HomeId is required");

        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("PermissionId is required");
    }
}