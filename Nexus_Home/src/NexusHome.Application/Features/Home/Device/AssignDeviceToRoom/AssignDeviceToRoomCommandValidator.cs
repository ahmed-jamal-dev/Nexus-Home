namespace NexusHome.Application.Features.Home.Device.AssignDeviceToRoom;

using FluentValidation;

/// <summary>
/// Validator for AssignDeviceToRoomCommand.
/// </summary>
public sealed class AssignDeviceToRoomCommandValidator : AbstractValidator<AssignDeviceToRoomCommand>
{
    public AssignDeviceToRoomCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.DeviceId)
            .NotEqual(Guid.Empty).WithMessage("Device ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}