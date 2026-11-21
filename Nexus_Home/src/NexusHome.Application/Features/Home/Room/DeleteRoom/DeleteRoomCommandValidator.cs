namespace NexusHome.Application.Features.Home.Room.DeleteRoom;

using FluentValidation;

/// <summary>
/// Validator for DeleteRoomCommand.
/// </summary>
public sealed class DeleteRoomCommandValidator : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.RoomId)
            .NotEqual(Guid.Empty).WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}