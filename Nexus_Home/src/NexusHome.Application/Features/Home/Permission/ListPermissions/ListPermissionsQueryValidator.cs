namespace NexusHome.Application.Features.Home.Permission.ListPermissions;

using FluentValidation;
using NexusHome.Domain.Enums;

/// <summary>
/// Validator for ListPermissionsQuery.
/// </summary>
public sealed class ListPermissionsQueryValidator : AbstractValidator<ListPermissionsQuery>
{
    public ListPermissionsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("HomeId is required");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Limit must not exceed 100");

        RuleFor(x => x.TargetType)
            .Must(BeValidTargetType).When(x => !string.IsNullOrEmpty(x.TargetType))
            .WithMessage("Invalid target type");

        RuleFor(x => x.GranteeMembershipId)
            .NotEmpty().When(x => x.GranteeMembershipId.HasValue)
            .WithMessage("GranteeMembershipId must not be empty if provided");
    }

    private static bool BeValidTargetType(string targetType)
    {
        return Enum.TryParse<TargetType>(targetType, true, out _);
    }
}