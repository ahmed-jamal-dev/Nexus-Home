namespace NexusHome.Application.Features.Home.Room.GetRoom;

using FluentValidation;

/// <summary>
/// Validator for GetRoomQuery.
/// </summary>
public sealed class GetRoomQueryValidator : AbstractValidator<GetRoomQuery>
{
    public GetRoomQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.RoomId)
            .NotEqual(Guid.Empty).WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}