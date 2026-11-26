namespace NexusHome.Api.Endpoints.Home;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusHome.Api.Filters;
using NexusHome.Application.Features.Home.Room.CreateRoom;
using NexusHome.Application.Features.Home.Room.DeleteRoom;
using NexusHome.Application.Features.Home.Room.GetRoom;
using NexusHome.Application.Features.Home.Room.ListRooms;
using NexusHome.Application.Features.Home.Room.UpdateRoom;

/// <summary>
/// Room management endpoint registrations.
/// </summary>
public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/homes/{homeId:guid}/rooms")
            .WithTags("Room Management")
            .WithOpenApi()
            .RequireRateLimiting("Default")
            .AddEndpointFilter<PlatformRolesVersionFilter>();

        // Create room - Requires HomeManage permission or Owner
        group.MapPost("/", CreateRoomAsync)
            .WithName("CreateRoom")
            .RequireAuthorization("Permission:HomeManage")
            .WithSummary("Create a room in a home")
            .WithDescription("Creates a new room with a unique name within the home. Requires HomeManage permission on the home or Owner role.")
            .Produces<CreateRoomResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // List rooms - Any member (Owner, Member, Guest)
        group.MapGet("/", ListRoomsAsync)
            .WithName("ListRooms")
            .RequireAuthorization("HomeAnyMember")
            .WithSummary("List rooms in a home")
            .WithDescription("Returns a paginated list of rooms for the home. Requires home membership (Owner, Member, or Guest).")
            .Produces<ListRoomsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Get room by ID - Any member (Owner, Member, Guest)
        group.MapGet("/{roomId:guid}", GetRoomAsync)
            .WithName("GetRoom")
            .RequireAuthorization("HomeAnyMember")
            .WithSummary("Get a room by ID")
            .WithDescription("Retrieves a room by its ID. Requires home membership (Owner, Member, or Guest).")
            .Produces<GetRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Update room name - Requires HomeManage permission or Owner
        group.MapPut("/{roomId:guid}", UpdateRoomAsync)
            .WithName("UpdateRoom")
            .RequireAuthorization("Permission:HomeManage")
            .WithSummary("Update a room name")
            .WithDescription("Updates the name of a room. Requires HomeManage permission on the home or Owner role.")
            .Produces<UpdateRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Soft delete room - Requires HomeManage permission or Owner
        group.MapDelete("/{roomId:guid}", DeleteRoomAsync)
            .WithName("DeleteRoom")
            .RequireAuthorization("Permission:HomeManage")
            .WithSummary("Delete a room")
            .WithDescription("Soft deletes a room, removing it from the home. Associated devices will have their room assignment cleared. Requires home membership (Owner or Member).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateRoomAsync(
        Guid homeId,
        [FromBody] CreateRoomRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateRoomCommand(userId.Value, homeId, request.Name);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{homeId}/rooms/{result.Value.Room.Id}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> ListRoomsAsync(
        Guid homeId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListRoomsQuery(userId.Value, homeId, cursor, limit);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetRoomAsync(
        Guid homeId,
        Guid roomId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetRoomQuery(userId.Value, homeId, roomId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> UpdateRoomAsync(
        Guid homeId,
        Guid roomId,
        [FromBody] UpdateRoomRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateRoomCommand(userId.Value, homeId, roomId, request.Name);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> DeleteRoomAsync(
        Guid homeId,
        Guid roomId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteRoomCommand(userId.Value, homeId, roomId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
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

/// <summary>
/// Request DTO for creating a room.
/// </summary>
public sealed record CreateRoomRequest(
    string Name);

/// <summary>
/// Request DTO for updating a room.
/// </summary>
public sealed record UpdateRoomRequest(
    string Name);