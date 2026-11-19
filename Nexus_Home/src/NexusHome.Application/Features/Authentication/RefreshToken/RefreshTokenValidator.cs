namespace NexusHome.Application.Features.Authentication.RefreshToken;

using FluentValidation;
using NexusHome.Application.Common;

/// <summary>
/// Validator for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode(Errors.Validation.RefreshTokenRequired.Code);
    }
}