namespace NexusHome.Api.Endpoints.Home;

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexusHome.Api.Filters;
using NexusHome.Application.Features.Home.Member.AcceptInvitation;
using NexusHome.Application.Features.Home.Member.ChangeMemberRole;
using NexusHome.Application.Features.Home.Member.InviteMember;
using NexusHome.Application.Features.Home.Member.ListMembers;
using NexusHome.Application.Features.Home.Member.RemoveMember;

/// <summary>
/// Member management endpoint registrations.
/// </summary>
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        // Invitation endpoints (public accept, authenticated invite)
        var invitationGroup = app.MapGroup("/api/v1/invitations")
            .WithTags("Member Invitations")
            .WithOpenApi()
            .AddEndpointFilter<PlatformRolesVersionFilter>();

        // Accept invitation (public - requires token)
        invitationGroup.MapPost("/accept", AcceptInvitationAsync)
            .WithName("AcceptInvitation")
            .WithSummary("Accept a member invitation")
            .WithDescription("Accepts a member invitation using the token. Returns the created membership. Requires the invitee to be authenticated.")
            .Produces<AcceptInvitationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Member management endpoints (home-scoped, require authentication)
        var memberGroup = app.MapGroup("/api/v1/homes/{homeId:guid}/members")
            .WithTags("Member Management")
            .WithOpenApi()
            .RequireAuthorization()
            .RequireRateLimiting("Default")
            .AddEndpointFilter<PlatformRolesVersionFilter>();

        // Invite member
        memberGroup.MapPost("/", InviteMemberAsync)
            .WithName("InviteMember")
            .WithSummary("Invite a member to a home")
            .WithDescription("Invites a user by email to join the home as a Member or Guest. Returns the plaintext invitation token ONCE. Only home Owner can invite.")
            .Produces<InviteMemberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // List members
        memberGroup.MapGet("/", ListMembersAsync)
            .WithName("ListMembers")
            .WithSummary("List members of a home")
            .WithDescription("Returns a paginated list of active members for the home. Requires home membership (Owner, Member, or Guest).")
            .Produces<ListMembersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Change member role
        memberGroup.MapPut("/{membershipId:guid}/role", ChangeMemberRoleAsync)
            .WithName("ChangeMemberRole")
            .WithSummary("Change a member's role")
            .WithDescription("Changes a member's role between Member and Guest. Owner role changes are blocked. Only home Owner can change roles.")
            .Produces<ChangeMemberRoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Remove member
        memberGroup.MapDelete("/{membershipId:guid}", RemoveMemberAsync)
            .WithName("RemoveMember")
            .WithSummary("Remove a member from a home")
            .WithDescription("Removes a member from the home (deactivates membership). Cannot remove the last active Owner. Only home Owner can remove members.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> InviteMemberAsync(
        Guid homeId,
        [FromBody] InviteMemberRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new InviteMemberCommand(userId.Value, homeId, request.Email, request.Role);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/homes/{homeId}/members", result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> ListMembersAsync(
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

        var query = new ListMembersQuery(userId.Value, homeId, cursor, limit);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> ChangeMemberRoleAsync(
        Guid homeId,
        Guid membershipId,
        [FromBody] ChangeMemberRoleRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new ChangeMemberRoleCommand(userId.Value, homeId, membershipId, request.Role);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid homeId,
        Guid membershipId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new RemoveMemberCommand(userId.Value, homeId, membershipId);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : HandleError(result.Error);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        [FromBody] AcceptInvitationRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        var command = new AcceptInvitationCommand(userId.Value, request.Token);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleError(result.Error);
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

    private static IResult HandleError(NexusHome.Application.Common.Error error)
    {
        var statusCode = error.Code switch
        {
            var c when c.StartsWith("AUTH.") => StatusCodes.Status401Unauthorized,
            var c when c.StartsWith("VALIDATION.") => StatusCodes.Status400BadRequest,
            var c when c.StartsWith("MEMBER.") => StatusCodes.Status400BadRequest,
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
/// Request DTOs for endpoints.
/// </summary>
public sealed record InviteMemberRequest(string Email, string Role);
public sealed record ChangeMemberRoleRequest(string Role);
public sealed record AcceptInvitationRequest(string Token);