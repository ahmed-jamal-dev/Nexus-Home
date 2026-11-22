namespace NexusHome.Application.Features.Home.Device.ListDevices;

using FluentValidation;

/// <summary>
/// Validator for ListDevicesQuery.
/// </summary>
public sealed class ListDevicesQueryValidator : AbstractValidator<ListDevicesQuery>
{
    public ListDevicesQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100");
    }
}