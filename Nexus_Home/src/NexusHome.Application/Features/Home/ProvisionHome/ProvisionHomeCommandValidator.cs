namespace NexusHome.Application.Features.Home.ProvisionHome;

using FluentValidation;

/// <summary>
/// Validator for ProvisionHomeCommand.
/// </summary>
public sealed class ProvisionHomeCommandValidator : AbstractValidator<ProvisionHomeCommand>
{
    private static readonly string[] ValidTimezones = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToArray();

    public ProvisionHomeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Home name is required")
            .MaximumLength(255).WithMessage("Home name must not exceed 255 characters");

        RuleFor(x => x.AddressLine1)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.AddressLine1))
            .WithMessage("Address line 1 must not exceed 255 characters");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.AddressLine2))
            .WithMessage("Address line 2 must not exceed 255 characters");

        RuleFor(x => x.City)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.City))
            .WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.StateProvince))
            .WithMessage("State/Province must not exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PostalCode))
            .WithMessage("Postal code must not exceed 20 characters");

        RuleFor(x => x.Timezone)
            .Must(tz => string.IsNullOrEmpty(tz) || ValidTimezones.Contains(tz))
            .WithMessage("Invalid timezone. Must be a valid IANA timezone identifier (e.g., 'America/New_York', 'UTC')");
    }
}