namespace NexusHome.Application.Features.Home.Device.AddDevice;

using FluentValidation;

/// <summary>
/// Validator for AddDeviceCommand.
/// </summary>
public sealed class AddDeviceCommandValidator : AbstractValidator<AddDeviceCommand>
{
    public AddDeviceCommandValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("External ID is required")
            .MaximumLength(100).WithMessage("External ID must not exceed 100 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device name is required")
            .MaximumLength(255).WithMessage("Device name must not exceed 255 characters");

        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Device type is required")
            .MaximumLength(100).WithMessage("Device type must not exceed 100 characters");

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

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}