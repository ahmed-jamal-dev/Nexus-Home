namespace NexusHome.Api.Filters;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusHome.Infrastructure.Data;

/// <summary>
/// Endpoint filter that validates the platform roles version (prv claim) against the database.
/// If the version in the token doesn't match the database, returns 401 Unauthorized with a specific error code.
/// This enables token invalidation when platform roles change.
/// </summary>
public sealed class PlatformRolesVersionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Skip validation for public endpoints
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
        {
            return await next(context);
        }

        // Skip for authentication endpoints (login, register, refresh)
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase))
        {
            return await next(context);
        }

        // Get the prv claim from the token
        var prvClaim = context.HttpContext.User.FindFirst("prv");
        if (prvClaim == null || !int.TryParse(prvClaim.Value, out var tokenVersion))
        {
            // No prv claim or invalid - allow through (will be caught by auth if needed)
            return await next(context);
        }

        // Get user ID from token
        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.HttpContext.User.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return await next(context);
        }

        // Get database context
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<NexusHomeDbContext>();

        // Get current version from database
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            // User not found - allow through (will be caught by auth if needed)
            return await next(context);
        }

        // Compare versions
        if (user.PlatformRolesVersion != tokenVersion)
        {
            // Version mismatch - token is stale
            var problemDetails = new ProblemDetails
            {
                Title = "TOKEN_VERSION_MISMATCH",
                Detail = "Token platform roles version is outdated. Please re-authenticate.",
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc9457"
            };
            problemDetails.Extensions["code"] = "AUTH.TOKEN_VERSION_MISMATCH";

            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails);
            return null;
        }

        return await next(context);
    }
}