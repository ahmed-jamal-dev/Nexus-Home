namespace NexusHome.Application.Features.Home.Member.ListMembers;

using FluentValidation;

/// <summary>
/// Validator for ListMembersQuery.
/// </summary>
public sealed class ListMembersQueryValidator : AbstractValidator<ListMembersQuery>
{
    public ListMembersQueryValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("Home ID is required");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Limit must not exceed 100");
    }
}