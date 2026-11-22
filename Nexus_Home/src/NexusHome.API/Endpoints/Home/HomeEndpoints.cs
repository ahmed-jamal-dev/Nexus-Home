namespace NexusHome.Api.Endpoints.Home;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusHome.Application.Features.Home.ClaimHome;
using NexusHome.Application.Features.Home.GenerateClaimCredential;
using NexusHome.Application.Features.Home.GetClaimCredential;
using NexusHome.Application.Features.Home.ProvisionHome;
using NexusHome.Application.Features.Home.RevokeClaimCredential;

/// <summary>
/// Home provisioning and ownership claim endpoint registrations.
/// </summary>
public static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomeEndpoints(this IEndpointRouteBuilder app)
    {
        // Provisioning endpoints (require HomeProvisioning policy)
        var provisioningGroup = app.MapGroup("/api/v1/homes")
            .WithTags("Home Provisioning")
            .WithOpenApi()
            .RequireAuthorization("HomeProvisioning")
            .RequireRateLimiting("ProvisioningEndpoint");

        // Provision a new home
        provisioningGroup.MapPost("/provision", ProvisionHomeAsync)
            .WithName("ProvisionHome")
            .WithSummary("Provision a new home (internal)")
            .WithDescription("Creates a new unclaimed home. Requires provisioning privileges.")
            .Produces<ProvisionHomeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Generate claim credential
        provisioningGroup.MapPost("/{homeId:guid}/claim-credential", GenerateClaimCredentialAsync)
            .WithName("GenerateClaimCredential")
            .WithSummary("Generate a claim credential for a home (internal)")
            .WithDescription("Generates a secure claim credential for home ownership. Returns plaintext credential ONLY ONCE. Requires provisioning privileges.")
            .Produces<GenerateClaimCredentialResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Get claim credential metadata (never returns plaintext)
        provisioningGroup.MapGet("/{homeId:guid}/claim-credential", GetClaimCredentialAsync)
            .WithName("GetClaimCredential")
            .WithSummary("Get claim credential metadata (internal)")
            .WithDescription("Retrieves claim credential metadata (expiry, status). NEVER returns the plaintext credential. Requires provisioning privileges.")
            .Produces<GetClaimCredentialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Revoke claim credential
        provisioningGroup.MapDelete("/{homeId:guid}/claim-credential", RevokeClaimCredentialAsync)
            .WithName("RevokeClaimCredential")
            .WithSummary("Revoke a claim credential (internal)")
            .WithDescription("Revokes the claim credential for a home, making it unusable. Requires provisioning privileges.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Claim home endpoint (authenticated user, rate limited)
        var claimGroup = app.MapGroup("/api/v1/homes")
            .WithTags("Home Ownership")
            .WithOpenApi()
            .RequireAuthorization()
            .RequireRateLimiting("ClaimEndpoint");

        claimGroup.MapPost("/claim", ClaimHomeAsync)
            .WithName("ClaimHome")
            .WithSummary("Claim home ownership")
            .WithDescription("Claims ownership of a home using a valid claim credential. Creates an Owner membership for the authenticated user.")
            .Produces<ClaimHomeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone);

        return app;
    }

    private static async Task<IResult> ProvisionHomeAsync(
        HttpContext context,
        [FromBody] ProvisionHomeRequest request,
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

        var command = new ProvisionHomeCommand(
            userId,
            request.Name,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateProvince,
            request.PostalCode,
            request.Timezone);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{result.Value.Home.Id}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GenerateClaimCredentialAsync(
        HttpContext context,
        Guid homeId,
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

        var command = new GenerateClaimCredentialCommand(userId, homeId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{homeId}/claim-credential", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetClaimCredentialAsync(
        HttpContext context,
        Guid homeId,
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

        var query = new GetClaimCredentialQuery(userId, homeId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> RevokeClaimCredentialAsync(
        HttpContext context,
        Guid homeId,
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

        var command = new RevokeClaimCredentialCommand(userId, homeId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : HandleError(result.Error);
    }

    private static async Task<IResult> ClaimHomeAsync(
        HttpContext context,
        [FromBody] ClaimHomeRequest request,
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

        var command = new ClaimHomeCommand(userId, request.Credential);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static IResult HandleError(NexusHome.Application.Common.Error error)
    {
        var statusCode = error.Code switch
        {
            // Validation errors
            "VALIDATION.EMAIL_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.EMAIL_INVALID" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORD_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORD_TOO_SHORT" => StatusCodes.Status400BadRequest,
            "VALIDATION.FULL_NAME_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.CONFIRM_PASSWORD_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.PASSWORDS_DO_NOT_MATCH" => StatusCodes.Status400BadRequest,
            "VALIDATION.REFRESH_TOKEN_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.CLAIM_CREDENTIAL_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION.CLAIM_CREDENTIAL_INVALID" => StatusCodes.Status400BadRequest,

            // Auth errors
            "AUTH.INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "AUTH.USER_NOT_FOUND" => StatusCodes.Status404NotFound,
            "AUTH.EMAIL_ALREADY_EXISTS" => StatusCodes.Status409Conflict,
            "AUTH.USER_INACTIVE" => StatusCodes.Status401Unauthorized,
            "AUTH.EMAIL_NOT_VERIFIED" => StatusCodes.Status401Unauthorized,
            "AUTH.INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
            "AUTH.REFRESH_TOKEN_REVOKED" => StatusCodes.Status401Unauthorized,
            "AUTH.REFRESH_TOKEN_EXPIRED" => StatusCodes.Status401Unauthorized,
            "AUTH.TOKEN_GENERATION_FAILED" => StatusCodes.Status500InternalServerError,

            // Home errors
            "HOME.NOT_FOUND" => StatusCodes.Status404NotFound,
            "HOME.DELETED" => StatusCodes.Status410Gone,
            "HOME.ALREADY_CLAIMED" => StatusCodes.Status409Conflict,
            "HOME.USER_ALREADY_OWNS" => StatusCodes.Status409Conflict,
            "HOME.CLAIM_CREDENTIAL_NOT_FOUND" => StatusCodes.Status404NotFound,
            "HOME.CLAIM_CREDENTIAL_EXPIRED" => StatusCodes.Status400BadRequest,
            "HOME.CLAIM_CREDENTIAL_ALREADY_USED" => StatusCodes.Status409Conflict,
            "HOME.CLAIM_CREDENTIAL_REVOKED" => StatusCodes.Status410Gone,
            "HOME.CLAIM_CREDENTIAL_EXISTS" => StatusCodes.Status409Conflict,
            "HOME.PROVISIONING_FORBIDDEN" => StatusCodes.Status403Forbidden,

            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}

/// <summary>
/// Request DTO for provisioning a home.
/// </summary>
public sealed record ProvisionHomeRequest(
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? StateProvince = null,
    string? PostalCode = null,
    string? Timezone = null);

/// <summary>
/// Request DTO for claiming a home.
/// </summary>
public sealed record ClaimHomeRequest(
    string Credential);