namespace NexusHome.Application.Common;

/// <summary>
/// Represents the result of an operation.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error _error;

    private Result(T value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if successful, throws if failed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value on a failed result.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed result.");

    /// <summary>
    /// Gets the error if failed, throws if successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing error on a successful result.</exception>
    public Error Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error on a successful result.");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Maps the success value using a function.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> func) => IsSuccess ? Result<TOut>.Success(func(_value!)) : Result<TOut>.Failure(_error);

    /// <summary>
    /// Maps the success value using an async function.
    /// </summary>
    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> func) => IsSuccess ? Result<TOut>.Success(await func(_value!)) : Result<TOut>.Failure(_error);
}

/// <summary>
/// Represents a result without a value (void result).
/// </summary>
public readonly struct Result
{
    private readonly Error _error;

    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    public Result()
    {
        _error = default;
        IsSuccess = true;
    }

    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if failed, throws if successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing error on a successful result.</exception>
    public Error Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access Error on a successful result.");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public readonly record struct Error(string Code, string Message)
{
    /// <summary>
    /// Creates an error from a code and formatted message.
    /// </summary>
    public static Error Create(string code, string message) => new(code, message);

    /// <summary>
    /// Creates an error from a code, message format, and arguments.
    /// </summary>
    public static Error Create(string code, string format, params object[] args) => new(code, string.Format(format, args));
}

/// <summary>
/// Common error codes.
/// </summary>
public static class Errors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = new("AUTH.INVALID_CREDENTIALS", "Invalid email or password");
        public static readonly Error UserNotFound = new("AUTH.USER_NOT_FOUND", "User not found");
        public static readonly Error EmailAlreadyExists = new("AUTH.EMAIL_ALREADY_EXISTS", "An account with this email already exists");
        public static readonly Error UserInactive = new("AUTH.USER_INACTIVE", "This account has been deactivated");
        public static readonly Error EmailNotVerified = new("AUTH.EMAIL_NOT_VERIFIED", "Please verify your email before logging in");
        public static readonly Error InvalidRefreshToken = new("AUTH.INVALID_REFRESH_TOKEN", "Invalid or expired refresh token");
        public static readonly Error RefreshTokenRevoked = new("AUTH.REFRESH_TOKEN_REVOKED", "This refresh token has been revoked");
        public static readonly Error RefreshTokenExpired = new("AUTH.REFRESH_TOKEN_EXPIRED", "This refresh token has expired");
        public static readonly Error TokenGenerationFailed = new("AUTH.TOKEN_GENERATION_FAILED", "Failed to generate authentication tokens");
    }

    public static class Validation
    {
        public static readonly Error EmailRequired = new("VALIDATION.EMAIL_REQUIRED", "Email is required");
        public static readonly Error EmailInvalid = new("VALIDATION.EMAIL_INVALID", "Invalid email format");
        public static readonly Error PasswordRequired = new("VALIDATION.PASSWORD_REQUIRED", "Password is required");
        public static readonly Error PasswordTooShort = new("VALIDATION.PASSWORD_TOO_SHORT", "Password must be at least 8 characters");
        public static readonly Error FullNameRequired = new("VALIDATION.FULL_NAME_REQUIRED", "Full name is required");
        public static readonly Error RefreshTokenRequired = new("VALIDATION.REFRESH_TOKEN_REQUIRED", "Refresh token is required");
        public static readonly Error ConfirmPasswordRequired = new("VALIDATION.CONFIRM_PASSWORD_REQUIRED", "Password confirmation is required");
        public static readonly Error PasswordsDoNotMatch = new("VALIDATION.PASSWORDS_DO_NOT_MATCH", "Passwords do not match");
        public static readonly Error ClaimCredentialRequired = new("VALIDATION.CLAIM_CREDENTIAL_REQUIRED", "Claim credential is required");
        public static readonly Error ClaimCredentialInvalid = new("VALIDATION.CLAIM_CREDENTIAL_INVALID", "Invalid claim credential format");
    }

    public static class Home
    {
        public static readonly Error HomeNotFound = new("HOME.NOT_FOUND", "Home not found");
        public static readonly Error HomeDeleted = new("HOME.DELETED", "Home has been deleted");
        public static readonly Error HomeAlreadyClaimed = new("HOME.ALREADY_CLAIMED", "Home already has an owner");
        public static readonly Error UserAlreadyOwnsHome = new("HOME.USER_ALREADY_OWNS", "User already owns this home");

        public static readonly Error ClaimCredentialNotFound = new("HOME.CLAIM_CREDENTIAL_NOT_FOUND", "Claim credential not found");
        public static readonly Error ClaimCredentialExpired = new("HOME.CLAIM_CREDENTIAL_EXPIRED", "Claim credential has expired");
        public static readonly Error ClaimCredentialAlreadyUsed = new("HOME.CLAIM_CREDENTIAL_ALREADY_USED", "Claim credential has already been used");
        public static readonly Error ClaimCredentialRevoked = new("HOME.CLAIM_CREDENTIAL_REVOKED", "Claim credential has been revoked");
        public static readonly Error ClaimCredentialExists = new("HOME.CLAIM_CREDENTIAL_EXISTS", "A valid claim credential already exists for this home");

        public static readonly Error ProvisioningForbidden = new("HOME.PROVISIONING_FORBIDDEN", "Provisioning requires elevated privileges");
    }
}