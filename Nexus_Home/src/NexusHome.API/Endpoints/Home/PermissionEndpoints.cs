namespace NexusHome.Api.Endpoints.Home;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using NexusHome.Api.Authorization.Handlers;
using NexusHome.Api.Authorization.Requirements;
using NexusHome.Api.Filters;
using NexusHome.Application.Common;
using NexusHome.Application.Features.Home.Permission.GrantPermission;
using NexusHome.Application.Features.Home.Permission.ListPermissions;
using NexusHome.Application.Features.Home.Permission.RevokePermission;

/// <summary>
/// Permission management endpoint registrations.
/// </summary>
public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var permissionGroup = app.MapGroup("/api/v1/homes/{homeId:guid}/permissions")
            .WithTags("Permission Management")
            .WithOpenApi()
            .RequireRateLimiting("Default")
            .AddEndpointFilter<PlatformRolesVersionFilter>();

        // Grant permission - Requires MemberManage permission or Owner
        permissionGroup.MapPost("/", GrantPermissionAsync)
            .WithName("GrantPermission")
            .RequireAuthorization("Permission:MemberManage")
            .WithSummary("Grant a permission to a member")
            .WithDescription("Grants a permission (Grant or Deny) to a member on a specific target (device, home, room, camera, microphone). Requires MemberManage permission on the home or Owner role.")
            .Produces<GrantPermissionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // List permissions - Requires MemberManage permission or Owner
        permissionGroup.MapGet("/", ListPermissionsAsync)
            .WithName("ListPermissions")
            .RequireAuthorization("Permission:MemberManage")
            .WithSummary("List permissions for a home or member")
            .WithDescription("Returns a paginated list of permissions for the home. Optionally filter by grantee membership, target type, or target ID. Requires MemberManage permission on the home or Owner role.")
            .Produces<ListPermissionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Revoke permission - Requires MemberManage permission or Owner
        permissionGroup.MapDelete("/{permissionId:guid}", RevokePermissionAsync)
            .WithName("RevokePermission")
            .RequireAuthorization("Permission:MemberManage")
            .WithSummary("Revoke a permission from a member")
            .WithDescription("Revokes a previously granted permission. Requires MemberManage permission on the home or Owner role.")
            .Produces<RevokePermissionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Test endpoint for DI resolution
        permissionGroup.MapGet("/test-di", TestDIAsync)
            .WithName("TestDI")
            .WithSummary("Test DI resolution for HasPermissionHandler")
            .AllowAnonymous()
            .Produces<string>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> TestDIAsync(
        HttpContext httpContext,
        ILogger<HasPermissionHandler> logger)
    {
        // Try to resolve from DI
        var handler = httpContext.RequestServices.GetService<HasPermissionHandler>();
        var authHandlers = httpContext.RequestServices.GetServices<Microsoft.AspNetCore.Authorization.IAuthorizationHandler>();

        logger.LogInformation("TestDI: Handler resolved: {Handler}", handler);
        logger.LogInformation("TestDI: Auth handlers count: {Count}", authHandlers.Count());

        foreach (var h in authHandlers)
        {
            logger.LogInformation("TestDI: Auth handler type: {Type}", h.GetType().FullName);
        }

        return Results.Ok(new {
            handlerResolved = handler != null,
            authHandlerCount = authHandlers.Count(),
            handlerTypes = authHandlers.Select(h => h.GetType().FullName).ToList()
        });
    }

    private static async Task<IResult> GrantPermissionAsync(
        Guid homeId,
        [FromBody] GrantPermissionRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken ct)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new GrantPermissionCommand(
            userId.Value,
            homeId,
            request.GranteeMembershipId,
            request.PermissionType,
            request.TargetType,
            request.TargetId,
            request.Effect);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{homeId}/permissions/{result.Value.PermissionId}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> ListPermissionsAsync(
        Guid homeId,
        IMediator mediator,
        HttpContext context,
        CancellationToken ct,
        [FromQuery] Guid? granteeMembershipId = null,
        [FromQuery] string? targetType = null,
        [FromQuery] Guid? targetId = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListPermissionsQuery(
            UserId: userId.Value,
            HomeId: homeId,
            GranteeMembershipId: granteeMembershipId,
            TargetType: targetType,
            TargetId: targetId,
            Cursor: cursor,
            Limit: limit);

        var result = await mediator.Send(query, ct);

        return result.IsSuccess ? Results.Ok(result.Value) : HandleError(result.Error);
    }

    private static async Task<IResult> RevokePermissionAsync(
        Guid homeId,
        Guid permissionId,
        IMediator mediator,
        HttpContext context,
        CancellationToken ct)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new RevokePermissionCommand(userId.Value, homeId, permissionId);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess ? Results.Ok(result.Value) : HandleError(result.Error);
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    private static IResult HandleError(Error error)
    {
        var statusCode = error.Code switch
        {
            var c when c.StartsWith("AUTH.") => StatusCodes.Status401Unauthorized,
            var c when c.StartsWith("VALIDATION.") => StatusCodes.Status400BadRequest,
            var c when c.StartsWith("PERMISSION.") => StatusCodes.Status400BadRequest,
            var c when c.StartsWith("HOME.") => StatusCodes.Status404NotFound,
            var c when c.StartsWith("PLATFORM.") => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { { "code", error.Code } });
    }
}

/// <summary>
/// Request DTOs for permission endpoints.
/// </summary>
public sealed record GrantPermissionRequest(
    Guid GranteeMembershipId,
    string PermissionType,
    string TargetType,
    Guid TargetId,
    string Effect = "Grant");