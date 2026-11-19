using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NexusHome.Infrastructure;
using NexusHome.Infrastructure.Data;

namespace NexusHome.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        // Configuration
        builder.Configuration.AddEnvironmentVariables(prefix: "CONNECTIONSTRINGS__");

        // Add Infrastructure services (DbContext, etc.)
        builder.Services.AddInfrastructure(builder.Configuration);

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                builder.Configuration.GetConnectionString("NexusHome")!,
                name: "postgresql",
                tags: new[] { "db", "sql", "ready" })
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis",
                tags: new[] { "cache", "ready" });

        // OpenAPI
        builder.Services.AddOpenApi();

        // Controllers/Minimal APIs
        builder.Services.AddControllers();

        // Global exception handling
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Global exception handler
        app.UseExceptionHandler();

        // Health check endpoints
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Liveness: no dependencies
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"), // Readiness: check dependencies
        });

        // API routes
        app.MapControllers();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Ensure database is created (for development)
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusHomeDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await app.RunAsync();
    }
}

// Global exception handler
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = exception.ToString(),
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}