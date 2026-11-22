namespace NexusHome.Application.Features.Home.Device.GetDevice;

using FluentValidation;

/// <summary>
/// Validator for GetDeviceQuery.
/// </summary>
public sealed class GetDeviceQueryValidator : AbstractValidator<GetDeviceQuery>
{
    public GetDeviceQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.DeviceId)
            .NotEqual(Guid.Empty).WithMessage("Device ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}