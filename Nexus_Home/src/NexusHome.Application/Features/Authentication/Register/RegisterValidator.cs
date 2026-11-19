namespace NexusHome.Application.Features.Authentication.Register;

using FluentValidation;
using NexusHome.Application.Common;

/// <summary>
/// Validator for RegisterCommand.
/// </summary>
public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(Errors.Validation.EmailRequired.Code)
            .EmailAddress().WithErrorCode(Errors.Validation.EmailInvalid.Code);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(Errors.Validation.PasswordRequired.Code)
            .MinimumLength(8).WithErrorCode(Errors.Validation.PasswordTooShort.Code);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithErrorCode(Errors.Validation.ConfirmPasswordRequired.Code)
            .Equal(x => x.Password).WithErrorCode(Errors.Validation.PasswordsDoNotMatch.Code);

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode(Errors.Validation.FullNameRequired.Code)
            .MaximumLength(200);
    }
}