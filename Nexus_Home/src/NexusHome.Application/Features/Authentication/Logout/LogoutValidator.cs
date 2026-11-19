namespace NexusHome.Application.Features.Authentication.Logout;

using FluentValidation;
using NexusHome.Application.Common;

/// <summary>
/// Validator for LogoutCommand.
/// </summary>
public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode(Errors.Validation.RefreshTokenRequired.Code);
    }
}