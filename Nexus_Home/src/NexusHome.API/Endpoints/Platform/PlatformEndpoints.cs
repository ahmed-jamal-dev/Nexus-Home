namespace NexusHome.Api.Endpoints.Platform;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexusHome.Api.Filters;
using NexusHome.Application.Features.Platform.AuditLog;
using NexusHome.Application.Features.Platform.Config.GetPlatformConfig;
using NexusHome.Application.Features.Platform.Health;
using NexusHome.Application.Features.Platform.Home.DeleteHome;
using NexusHome.Application.Features.Platform.Home.GetHome;
using NexusHome.Application.Features.Platform.Home.GetHomeMemberships;
using NexusHome.Application.Features.Platform.Home.ListHomes;
using NexusHome.Application.Features.Platform.User.AnonymizeUser;
using NexusHome.Application.Features.Platform.User.AssignPlatformRole;
using NexusHome.Application.Features.Platform.User.CreateUser;
using NexusHome.Application.Features.Platform.User.GetUser;
using NexusHome.Application.Features.Platform.User.ListUsers;
using NexusHome.Application.Features.Platform.User.RevokePlatformRole;
using NexusHome.Application.Features.Platform.User.UpdateUserStatus;

/// <summary>
/// Platform administration endpoint registrations.
/// </summary>
public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        // Platform group with token version filter applied ONLY to platform endpoints
        var group = app.MapGroup("/api/v1/platform")
            .WithTags("Platform Administration")
            .WithOpenApi()
            .RequireAuthorization()
            .AddEndpointFilter<PlatformRolesVersionFilter>(); // Token version validation (scoped to platform)

        // User management - UserManage scope for mutating, UserRead for reading
        var userGroup = group.MapGroup("/users");

        // List users (UserRead scope)
        userGroup.MapGet("/", ListUsersAsync)
            .WithName("ListUsers")
            .WithSummary("List all users")
            .WithDescription("Returns a paginated list of users with filtering options. Requires UserRead scope.")
            .RequireAuthorization("UserRead")
            .Produces<ListUsersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Get user by ID (UserRead scope)
        userGroup.MapGet("/{userId:guid}", GetUserAsync)
            .WithName("GetUser")
            .WithSummary("Get user by ID")
            .WithDescription("Retrieves detailed user information including platform roles and home memberships. Requires UserRead scope.")
            .RequireAuthorization("UserRead")
            .Produces<GetUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Create user (UserManage scope)
        userGroup.MapPost("/", CreateUserAsync)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .WithDescription("Creates a new user account with optional platform role assignments. Requires UserManage scope.")
            .RequireAuthorization("UserManage")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Update user status (UserManage scope)
        userGroup.MapPut("/{userId:guid}/status", UpdateUserStatusAsync)
            .WithName("UpdateUserStatus")
            .WithSummary("Update user active status")
            .WithDescription("Activates or deactivates a user account. Cannot deactivate yourself or SuperAdmin users. Requires UserManage scope.")
            .RequireAuthorization("UserManage")
            .Produces<UpdateUserStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Assign platform role (UserManage scope)
        userGroup.MapPost("/{userId:guid}/roles", AssignPlatformRoleAsync)
            .WithName("AssignPlatformRole")
            .WithSummary("Assign a platform role to a user")
            .WithDescription("Assigns a platform role to a user with optional scope (e.g., Home for Technician). Requires UserManage scope.")
            .RequireAuthorization("UserManage")
            .Produces<AssignPlatformRoleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Revoke platform role (UserManage scope)
        userGroup.MapDelete("/{userId:guid}/roles/{roleKey}", RevokePlatformRoleAsync)
            .WithName("RevokePlatformRole")
            .WithSummary("Revoke a platform role from a user")
            .WithDescription("Revokes a platform role from a user. Cannot revoke SuperAdmin role unless requester is SuperAdmin. Requires UserManage scope.")
            .RequireAuthorization("UserManage")
            .Produces<RevokePlatformRoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Anonymize user (UserManage scope - handler enforces SuperAdmin only)
        userGroup.MapPost("/{userId:guid}/anonymize", AnonymizeUserAsync)
            .WithName("AnonymizeUser")
            .WithSummary("Anonymize a user (GDPR right to erasure)")
            .WithDescription("Anonymizes a user account for GDPR compliance. Only SuperAdmin can perform this action. Requires UserManage scope.")
            .RequireAuthorization("UserManage")
            .Produces<AnonymizeUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Home management - HomeRead scope for reading, HomeManage for mutating
        var homeGroup = group.MapGroup("/homes")
            .RequireAuthorization("HomeRead");

        // List homes (HomeRead scope)
        homeGroup.MapGet("/", ListHomesAsync)
            .WithName("ListHomes")
            .WithSummary("List all homes")
            .WithDescription("Returns a paginated list of homes. Technicians see only homes they provisioned. Requires HomeRead scope.")
            .Produces<ListHomesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Get home by ID (HomeRead scope)
        homeGroup.MapGet("/{homeId:guid}", GetHomeAsync)
            .WithName("GetHome")
            .WithSummary("Get home by ID")
            .WithDescription("Retrieves detailed home information including memberships, devices, and rooms. Technicians see only homes they provisioned. Requires HomeRead scope.")
            .Produces<GetHomeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Get home memberships (HomeManage scope)
        homeGroup.MapGet("/{homeId:guid}/memberships", GetHomeMembershipsAsync)
            .WithName("GetHomeMemberships")
            .WithSummary("Get home memberships")
            .WithDescription("Retrieves all memberships for a home with user platform roles. Requires HomeManage scope.")
            .RequireAuthorization("HomeManage")
            .Produces<GetHomeMembershipsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Delete home (HomeManage scope + self-protection in handler)
        var homeManageGroup = homeGroup
            .RequireAuthorization("HomeManage");

        homeManageGroup.MapDelete("/{homeId:guid}", DeleteHomeAsync)
            .WithName("DeleteHome")
            .WithSummary("Delete a home")
            .WithDescription("Soft deletes a home. Requires HomeManage scope.")
            .Produces<DeleteHomeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        // Platform health (PlatformHealth scope)
        group.MapGet("/health", GetPlatformHealthAsync)
            .WithName("GetPlatformHealth")
            .WithSummary("Get platform health")
            .WithDescription("Returns platform-wide statistics and health metrics. Requires PlatformHealth scope.")
            .RequireAuthorization("PlatformHealth")
            .Produces<GetPlatformHealthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Audit logs (PlatformAudit scope)
        group.MapGet("/audit-logs", ListPlatformAuditLogsAsync)
            .WithName("ListPlatformAuditLogs")
            .WithSummary("List platform audit logs")
            .WithDescription("Returns a paginated list of platform audit logs with filtering options. Requires PlatformAudit scope.")
            .RequireAuthorization("PlatformAudit")
            .Produces<ListPlatformAuditLogsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Platform configuration (PlatformConfig scope - SuperAdmin only)
        group.MapGet("/config", GetPlatformConfigAsync)
            .WithName("GetPlatformConfig")
            .WithSummary("Get platform configuration")
            .WithDescription("Returns platform configuration and settings. Requires PlatformConfig scope (SuperAdmin only).")
            .RequireAuthorization("PlatformConfig")
            .Produces<GetPlatformConfigResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    // User endpoints
    private static async Task<IResult> ListUsersAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? role = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListUsersQuery(userId.Value, search, isActive, role, cursor, limit);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetUserAsync(
        HttpContext context,
        Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var requesterId = GetUserId(context);
        if (requesterId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserQuery(requesterId.Value, userId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> CreateUserAsync(
        HttpContext context,
        [FromBody] CreateUserRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var roleAssignments = request.RoleAssignments
            .Select(ra => new NexusHome.Application.Features.Platform.User.CreateUser.CreateUserRoleAssignment(
                ra.RoleKey,
                ra.ScopeId,
                ra.ScopeType))
            .ToList();

        var command = new CreateUserCommand(
            userId.Value,
            request.Email,
            request.FullName,
            request.Password,
            roleAssignments);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/platform/users/{result.Value.UserId}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> UpdateUserStatusAsync(
        HttpContext context,
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var requesterId = GetUserId(context);
        if (requesterId == null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateUserStatusCommand(requesterId.Value, userId, request.IsActive);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> AssignPlatformRoleAsync(
        HttpContext context,
        Guid userId,
        [FromBody] AssignPlatformRoleRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var requesterId = GetUserId(context);
        if (requesterId == null)
        {
            return Results.Unauthorized();
        }

        var command = new AssignPlatformRoleCommand(
            requesterId.Value,
            userId,
            request.RoleKey,
            request.ScopeId,
            request.ScopeType,
            request.ExpiresAt);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/platform/users/{userId}/roles/{request.RoleKey}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> RevokePlatformRoleAsync(
        HttpContext context,
        Guid userId,
        string roleKey,
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] Guid? scopeId = null,
        [FromQuery] string? scopeType = null)
    {
        var requesterId = GetUserId(context);
        if (requesterId == null)
        {
            return Results.Unauthorized();
        }

        var command = new RevokePlatformRoleCommand(requesterId.Value, userId, roleKey, scopeId, scopeType);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> AnonymizeUserAsync(
        HttpContext context,
        Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var requesterId = GetUserId(context);
        if (requesterId == null)
        {
            return Results.Unauthorized();
        }

        var command = new AnonymizeUserCommand(requesterId.Value, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    // Home endpoints
    private static async Task<IResult> ListHomesAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListHomesQuery(userId.Value, search, isDeleted, cursor, limit);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetHomeAsync(
        HttpContext context,
        Guid homeId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetHomeQuery(userId.Value, homeId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> DeleteHomeAsync(
        HttpContext context,
        Guid homeId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteHomeCommand(userId.Value, homeId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetHomeMembershipsAsync(
        HttpContext context,
        Guid homeId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetHomeMembershipsQuery(userId.Value, homeId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    // Platform health endpoint
    private static async Task<IResult> GetPlatformHealthAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetPlatformHealthQuery(userId.Value);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    // Audit log endpoint
    private static async Task<IResult> ListPlatformAuditLogsAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] Guid? targetId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListPlatformAuditLogsQuery(
            userId.Value,
            actorUserId,
            action,
            targetType,
            targetId,
            from,
            to,
            cursor,
            limit);

        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    // Platform config endpoint
    private static async Task<IResult> GetPlatformConfigAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetPlatformConfigQuery(userId.Value);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        return userId;
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

            // Platform errors
            "PLATFORM.USER_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PLATFORM.USER_ALREADY_EXISTS" => StatusCodes.Status409Conflict,
            "PLATFORM.CANNOT_DEACTIVATE_SELF" => StatusCodes.Status400BadRequest,
            "PLATFORM.CANNOT_MODIFY_SUPER_ADMIN" => StatusCodes.Status403Forbidden,
            "PLATFORM.ROLE_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PLATFORM.ROLE_ALREADY_ASSIGNED" => StatusCodes.Status409Conflict,
            "PLATFORM.ROLE_ASSIGNMENT_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PLATFORM.CANNOT_REVOKE_SYSTEM_ROLE" => StatusCodes.Status403Forbidden,
            "PLATFORM.HOME_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PLATFORM.HOME_DELETED" => StatusCodes.Status410Gone,
            "PLATFORM.INSUFFICIENT_PRIVILEGE" => StatusCodes.Status403Forbidden,
            "PLATFORM.TOKEN_VERSION_MISMATCH" => StatusCodes.Status401Unauthorized,
            "PLATFORM.AUDIT_LOG_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PLATFORM.INVALID_SCOPE" => StatusCodes.Status400BadRequest,
            "PLATFORM.SCOPE_REQUIRED" => StatusCodes.Status400BadRequest,

            // Room errors
            "ROOM.NOT_FOUND" => StatusCodes.Status404NotFound,
            "ROOM.DUPLICATE_NAME" => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}

// Request DTOs
public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    List<CreateUserRoleAssignment> RoleAssignments);

public sealed record CreateUserRoleAssignment(
    string RoleKey,
    Guid? ScopeId = null,
    string? ScopeType = null);

public sealed record UpdateUserStatusRequest(
    bool IsActive);

public sealed record AssignPlatformRoleRequest(
    string RoleKey,
    Guid? ScopeId = null,
    string? ScopeType = null,
    DateTimeOffset? ExpiresAt = null);