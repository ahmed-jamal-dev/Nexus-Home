namespace NexusHome.Application.Features.Authentication.Login;

using FluentValidation;
using NexusHome.Application.Common;

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(Errors.Validation.EmailRequired.Code)
            .EmailAddress().WithErrorCode(Errors.Validation.EmailInvalid.Code);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(Errors.Validation.PasswordRequired.Code);
    }
}