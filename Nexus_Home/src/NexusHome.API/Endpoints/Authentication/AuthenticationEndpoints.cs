namespace NexusHome.Api.Endpoints.Authentication;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusHome.Application.Features.Authentication.GetCurrentUser;
using NexusHome.Application.Features.Authentication.Login;
using NexusHome.Application.Features.Authentication.Logout;
using NexusHome.Application.Features.Authentication.RefreshToken;
using NexusHome.Application.Features.Authentication.Register;

/// <summary>
/// Authentication endpoint registrations.
/// </summary>
public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Register
        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account and returns authentication tokens")
            .AllowAnonymous()
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Login
        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Log in a user")
            .WithDescription("Authenticates a user and returns access and refresh tokens")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Refresh Token
        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Uses a refresh token to obtain a new access token and rotated refresh token")
            .AllowAnonymous()
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Logout
        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Log out a user")
            .WithDescription("Revokes the provided refresh token")
            .AllowAnonymous()
            .Produces<LogoutResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Get Current User (requires authentication)
        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Get current user")
            .WithDescription("Returns the authenticated user's profile information")
            .RequireAuthorization()
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.ConfirmPassword, request.FullName);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/auth/me", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var query = new GetCurrentUserQuery(userId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static IResult HandleError(NexusHome.Application.Common.Error error)
    {
        var statusCode = error.Code switch
        {
            "VALIDATION.EMAIL_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.EMAIL_INVALID" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORD_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORD_TOO_SHORT" => StatusCodes.Status400BadRequest,
            "VALIDATION.FULL_NAME_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.CONFIRM_PASSWORD_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORDS_DO_NOT_MATCH" => StatusCodes.Status400BadRequest,
            "VALIDATION.REFRESH_TOKEN_REQUIRED" => StatusCodes.Status400BadRequest,
            "AUTH.INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "AUTH.USER_NOT_FOUND" => StatusCodes.Status404NotFound,
            "AUTH.EMAIL_ALREADY_EXISTS" => StatusCodes.Status409Conflict,
            "AUTH.USER_INACTIVE" => StatusCodes.Status401Unauthorized,
            "AUTH.EMAIL_NOT_VERIFIED" => StatusCodes.Status401Unauthorized,
            "AUTH.INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
            "AUTH.REFRESH_TOKEN_REVOKED" => StatusCodes.Status401Unauthorized,
            "AUTH.REFRESH_TOKEN_EXPIRED" => StatusCodes.Status401Unauthorized,
            "AUTH.TOKEN_GENERATION_FAILED" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode
        );
    }
}

// Request DTOs for endpoints
public sealed record RegisterRequest(string Email, string Password, string ConfirmPassword, string FullName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);