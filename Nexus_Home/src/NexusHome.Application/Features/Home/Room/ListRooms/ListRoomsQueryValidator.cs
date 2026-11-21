namespace NexusHome.Application.Features.Home.Room.ListRooms;

using FluentValidation;

/// <summary>
/// Validator for ListRoomsQuery.
/// </summary>
public sealed class ListRoomsQueryValidator : AbstractValidator<ListRoomsQuery>
{
    public ListRoomsQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Limit must not exceed 100");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor).When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Invalid cursor format");
    }

    private static bool BeValidCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return true;

        return Guid.TryParse(cursor, out _);
    }
}