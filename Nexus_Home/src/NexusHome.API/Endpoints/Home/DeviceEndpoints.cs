namespace NexusHome.Api.Endpoints.Home;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusHome.Api.Filters;
using NexusHome.Application.Features.Home.Device.AddDevice;
using NexusHome.Application.Features.Home.Device.AssignDeviceToRoom;
using NexusHome.Application.Features.Home.Device.GetDevice;
using NexusHome.Application.Features.Home.Device.ListDevices;
using NexusHome.Application.Features.Home.Device.RemoveDevice;
using NexusHome.Application.Features.Home.Device.UpdateDevice;

/// <summary>
/// Device management endpoint registrations.
/// </summary>
public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/homes/{homeId:guid}/devices")
            .WithTags("Device Management")
            .WithOpenApi()
            .RequireRateLimiting("Default")
            .AddEndpointFilter<PlatformRolesVersionFilter>();

        // Create device - Member or Owner
        group.MapPost("/", CreateDeviceAsync)
            .WithName("CreateDevice")
            .RequireAuthorization("HomeMemberOrOwner")
            .WithSummary("Add a device to a home")
            .WithDescription("Registers a new device with a globally unique ExternalId, capabilities, and metadata. Requires home membership (Owner or Member).")
            .Produces<AddDeviceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // List devices - Any member (Owner, Member, Guest)
        group.MapGet("/", ListDevicesAsync)
            .WithName("ListDevices")
            .RequireAuthorization("HomeAnyMember")
            .WithSummary("List devices in a home")
            .WithDescription("Returns a paginated list of devices for the home, optionally filtered by room. Requires home membership (Owner, Member, or Guest).")
            .Produces<ListDevicesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Get device by ID - Any member (Owner, Member, Guest)
        group.MapGet("/{deviceId:guid}", GetDeviceAsync)
            .WithName("GetDevice")
            .RequireAuthorization("HomeAnyMember")
            .WithSummary("Get a device by ID")
            .WithDescription("Retrieves a device by its ID with full details including reported state. Requires home membership (Owner, Member, or Guest).")
            .Produces<GetDeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Update device - Member or Owner
        group.MapPut("/{deviceId:guid}", UpdateDeviceAsync)
            .WithName("UpdateDevice")
            .RequireAuthorization("HomeMemberOrOwner")
            .WithSummary("Update a device")
            .WithDescription("Updates device name, device type, capabilities, metadata, firmware version, manufacturer, or model. Full replacement for capabilities and metadata. Requires home membership (Owner or Member).")
            .Produces<UpdateDeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Assign/Unassign device to room - Member or Owner
        group.MapPatch("/{deviceId:guid}/room", AssignDeviceToRoomAsync)
            .WithName("AssignDeviceToRoom")
            .RequireAuthorization("HomeMemberOrOwner")
            .WithSummary("Assign or unassign a device to/from a room")
            .WithDescription("Moves a device to a different room or unassigns it (null roomId). Room must belong to the same home. Requires home membership (Owner or Member).")
            .Produces<AssignDeviceToRoomResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Soft delete device - Member or Owner
        group.MapDelete("/{deviceId:guid}", RemoveDeviceAsync)
            .WithName("RemoveDevice")
            .RequireAuthorization("HomeMemberOrOwner")
            .WithSummary("Remove a device")
            .WithDescription("Soft deletes a device, excluding it from lists. RoomId is preserved on device removal. Requires home membership (Owner or Member).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateDeviceAsync(
        Guid homeId,
        [FromBody] CreateDeviceRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new AddDeviceCommand(
            userId.Value,
            homeId,
            request.ExternalId,
            request.Name,
            request.DeviceType,
            request.Capabilities,
            request.Metadata,
            request.FirmwareVersion,
            request.Manufacturer,
            request.Model);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{homeId}/devices/{result.Value.Device.Id}", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> ListDevicesAsync(
        Guid homeId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken,
        [FromQuery] Guid? roomId = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new ListDevicesQuery(userId.Value, homeId, roomId, cursor, limit);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> GetDeviceAsync(
        Guid homeId,
        Guid deviceId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var query = new GetDeviceQuery(userId.Value, homeId, deviceId);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> UpdateDeviceAsync(
        Guid homeId,
        Guid deviceId,
        [FromBody] UpdateDeviceRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateDeviceCommand(
            userId.Value,
            homeId,
            deviceId,
            request.Name,
            request.DeviceType,
            request.Capabilities,
            request.Metadata,
            request.FirmwareVersion,
            request.Manufacturer,
            request.Model);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> AssignDeviceToRoomAsync(
        Guid homeId,
        Guid deviceId,
        [FromBody] AssignDeviceToRoomRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new AssignDeviceToRoomCommand(userId.Value, homeId, deviceId, request.RoomId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> RemoveDeviceAsync(
        Guid homeId,
        Guid deviceId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new RemoveDeviceCommand(userId.Value, homeId, deviceId);
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

            // Device errors
            "DEVICE.NOT_FOUND" => StatusCodes.Status404NotFound,
            "DEVICE.REMOVED" => StatusCodes.Status410Gone,
            "DEVICE.DUPLICATE_EXTERNAL_ID" => StatusCodes.Status409Conflict,
            "DEVICE.INVALID_ROOM_ASSIGNMENT" => StatusCodes.Status400BadRequest,
            "DEVICE.ROOM_DELETED" => StatusCodes.Status400BadRequest,
            "DEVICE.BELONGS_TO_OTHER_HOME" => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}

/// <summary>
/// Request DTO for creating a device.
/// </summary>
public sealed record CreateDeviceRequest(
    string ExternalId,
    string Name,
    string DeviceType,
    System.Collections.Generic.Dictionary<string, object>? Capabilities = null,
    System.Collections.Generic.Dictionary<string, object>? Metadata = null,
    string? FirmwareVersion = null,
    string? Manufacturer = null,
    string? Model = null);

/// <summary>
/// Request DTO for updating a device.
/// </summary>
public sealed record UpdateDeviceRequest(
    string? Name = null,
    string? DeviceType = null,
    System.Collections.Generic.Dictionary<string, object>? Capabilities = null,
    System.Collections.Generic.Dictionary<string, object>? Metadata = null,
    string? FirmwareVersion = null,
    string? Manufacturer = null,
    string? Model = null);

/// <summary>
/// Request DTO for assigning a device to a room.
/// </summary>
public sealed record AssignDeviceToRoomRequest(
    Guid? RoomId);