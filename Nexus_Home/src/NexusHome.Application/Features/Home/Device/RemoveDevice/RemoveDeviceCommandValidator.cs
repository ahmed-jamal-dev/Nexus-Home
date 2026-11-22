namespace NexusHome.Application.Features.Home.Device.RemoveDevice;

using FluentValidation;

/// <summary>
/// Validator for RemoveDeviceCommand.
/// </summary>
public sealed class RemoveDeviceCommandValidator : AbstractValidator<RemoveDeviceCommand>
{
    public RemoveDeviceCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.DeviceId)
            .NotEqual(Guid.Empty).WithMessage("Device ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}