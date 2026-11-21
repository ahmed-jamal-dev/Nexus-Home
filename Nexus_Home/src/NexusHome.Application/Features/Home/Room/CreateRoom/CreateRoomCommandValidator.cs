namespace NexusHome.Application.Features.Home.Room.CreateRoom;

using FluentValidation;

/// <summary>
/// Validator for CreateRoomCommand.
/// </summary>
public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MaximumLength(100).WithMessage("Room name must not exceed 100 characters");

        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}