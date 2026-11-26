namespace NexusHome.Application.Features.Home.Permission.GrantPermission;

using FluentValidation;
using NexusHome.Domain.Enums;

/// <summary>
/// Validator for GrantPermissionCommand.
/// </summary>
public sealed class GrantPermissionCommandValidator : AbstractValidator<GrantPermissionCommand>
{
    public GrantPermissionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("HomeId is required");

        RuleFor(x => x.GranteeMembershipId)
            .NotEmpty().WithMessage("GranteeMembershipId is required");

        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("PermissionType is required")
            .Must(BeValidPermissionType).WithMessage("Invalid permission type");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("TargetType is required")
            .Must(BeValidTargetType).WithMessage("Invalid target type");

        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("TargetId is required");

        RuleFor(x => x.Effect)
            .NotEmpty().WithMessage("Effect is required")
            .Must(BeValidEffect).WithMessage("Effect must be 'Grant' or 'Deny'");
    }

    private static bool BeValidPermissionType(string permissionType)
    {
        return PermissionTypeExtensions.TryParseFromPgLabel(permissionType, out _);
    }

    private static bool BeValidTargetType(string targetType)
    {
        return TargetTypeExtensions.TryParseFromPgLabel(targetType, out _);
    }

    private static bool BeValidEffect(string effect)
    {
        return Enum.TryParse<Effect>(effect, true, out _);
    }
}