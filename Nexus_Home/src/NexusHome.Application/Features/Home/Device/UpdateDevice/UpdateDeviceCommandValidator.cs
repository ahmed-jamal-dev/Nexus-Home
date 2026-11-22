namespace NexusHome.Application.Features.Home.Device.UpdateDevice;

using FluentValidation;

/// <summary>
/// Validator for UpdateDeviceCommand.
/// </summary>
public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255).WithMessage("Device name must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.DeviceType)
            .MaximumLength(100).WithMessage("Device type must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DeviceType));

        RuleFor(x => x.FirmwareVersion)
            .MaximumLength(50).WithMessage("Firmware version must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.FirmwareVersion));

        RuleFor(x => x.Manufacturer)
            .MaximumLength(100).WithMessage("Manufacturer must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Manufacturer));

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Model));

        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.DeviceId)
            .NotEqual(Guid.Empty).WithMessage("Device ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}