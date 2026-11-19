using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexusHome.Api.Endpoints.Authentication;
using NexusHome.Application;
using NexusHome.Infrastructure;
using NexusHome.Infrastructure.Authentication;
using NexusHome.Infrastructure.Data;
using System.Text;

namespace NexusHome.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        // Configuration
        builder.Configuration.AddEnvironmentVariables(prefix: "CONNECTIONSTRINGS__");

        // Add Application services (MediatR, FluentValidation, Pipeline Behaviors)
        builder.Services.AddApplication();

        // Add Infrastructure services (DbContext, Repositories, Auth Services)
        builder.Services.AddInfrastructure(builder.Configuration);

        // Configure JWT Authentication
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Don't include error details in the response
                options.IncludeErrorDetails = false;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Don't log token details
                        context.NoResult();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(new
                        {
                            error = "Authentication failed",
                            message = "Invalid or expired token"
                        });
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(new
                        {
                            error = "Unauthorized",
                            message = "Authentication required"
                        });
                    }
                };
            });

        builder.Services.AddAuthorization();

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

        // OpenAPI (built-in for document generation)
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "Nexus Home API";
                document.Info.Version = "v1";
                document.Info.Description = "Nexus Home Smart Home API";

                // Add JWT Bearer security scheme
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter JWT Bearer token (e.g., 'eyJhbGciOiJIUzI1NiIs...')"
                };

                // Add global security requirement to all operations
                var securityRequirement = new OpenApiSecurityRequirement();
                securityRequirement.Add(new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>());

                foreach (var path in document.Paths.Values)
                {
                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(securityRequirement);
                    }
                }

                return Task.CompletedTask;
            });
        });

        // ProblemDetails for consistent error responses
        builder.Services.AddProblemDetails();

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        // Suppress EF Core verbose internal logs in development
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Model", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Nexus Home API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Nexus Home API";
                options.DefaultModelsExpandDepth(-1); // Hide models section by default
            });
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

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // API Endpoints
        app.MapAuthenticationEndpoints();

        app.UseHttpsRedirection();

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

// JWT Settings for configuration binding
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 15;
    public int RefreshTokenExpirationDays { get; init; } = 30;
    public string SigningKey { get; init; } = string.Empty;
}