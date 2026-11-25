using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexusHome.Api.Endpoints.Authentication;
using NexusHome.Api.Endpoints.Home;
using NexusHome.Api.Endpoints.Platform;
using NexusHome.Application.Interfaces;
using NexusHome.Infrastructure.Services;
using NexusHome.Api.Filters;
using NexusHome.Application;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure;
using NexusHome.Infrastructure.Authentication;
using NexusHome.Infrastructure.Data;
using System.Text;
using System.Threading.RateLimiting;

namespace NexusHome.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Check for bootstrap command
        if (args.Length > 0 && args[0] == "bootstrap-superadmin")
        {
            await RunBootstrapSuperAdminAsync(args);
            return;
        }

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

        // Validate SigningKey is present and meets minimum requirements
        if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
        {
            throw new InvalidOperationException(
                "JWT SigningKey is not configured. " +
                "Set it via environment variable 'Jwt__SigningKey' or user secrets. " +
                "Key must be at least 32 characters for HMAC-SHA256.");
        }

        if (jwtSettings.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT SigningKey is too short ({jwtSettings.SigningKey.Length} characters). " +
                "Minimum 32 characters required for HMAC-SHA256 security.");
        }

        // DIAGNOSTIC: Log JWT settings (without exposing the key)
        var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var diagLogger = loggerFactory.CreateLogger("JwtDiagnostic");
        diagLogger.LogInformation("JWT Settings loaded: Issuer={Issuer}, Audience={Audience}, SigningKeyLength={KeyLength}",
            jwtSettings.Issuer, jwtSettings.Audience, jwtSettings.SigningKey.Length);

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
                    OnMessageReceived = context =>
                    {
                        // DIAGNOSTIC: Log the raw Authorization header and extracted token
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        logger.LogInformation("OnMessageReceived: Authorization header = {AuthHeader}", authHeader);
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = authHeader.Substring("Bearer ".Length).Trim();
                            logger.LogInformation("OnMessageReceived: Extracted token (first 50 chars) = {Token}", token.Substring(0, Math.Min(50, token.Length)));
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Log the actual exception for debugging
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "JWT Authentication failed");

                        // Don't log token details
                        context.NoResult();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(new
                        {
                            error = "Authentication failed",
                            message = "Invalid or expired token",
                            details = context.Exception?.Message
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

        // Authorization policies
        builder.Services.AddAuthorization(options =>
        {
            // HomeProvisioning policy: requires scope:home.provision OR role:Technician
            options.AddPolicy("HomeProvisioning", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("home.provision")) ||
                    context.User.HasClaim(c => c.Type == "roles" && c.Value.Contains("Technician"))));

            // Platform administration role-based policies (for portal/UI access control)
            options.AddPolicy("SuperAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "roles" && c.Value.Contains("SuperAdmin"))));

            options.AddPolicy("PlatformAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "roles" && (c.Value.Contains("SuperAdmin") || c.Value.Contains("PlatformAdmin")))));

            options.AddPolicy("Technician", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "roles" && c.Value.Contains("Technician"))));

            // SupportAgent deferred to Phase 1.5 - policy not enabled in MVP

            // Scope-based policies for fine-grained API endpoint authorization
            options.AddPolicy("UserRead", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("user:read"))));

            options.AddPolicy("UserManage", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("user:manage"))));

            options.AddPolicy("HomeRead", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("home:read"))));

            options.AddPolicy("HomeManage", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("home:manage"))));

            options.AddPolicy("PlatformHealth", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("platform:health"))));

            options.AddPolicy("PlatformAudit", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("platform:audit"))));

            options.AddPolicy("PlatformConfig", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("platform:config"))));
        });

        // Rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            // Default limiter: 60 requests per minute per user
            options.AddFixedWindowLimiter("Default", limiterOptions =>
            {
                limiterOptions.PermitLimit = 60;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            // Claim endpoint: 10 requests per minute per IP + 5 per user
            options.AddFixedWindowLimiter("ClaimEndpoint", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            // Provisioning endpoints: 5 requests per minute per client
            options.AddFixedWindowLimiter("ProvisioningEndpoint", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // Platform roles version filter (validates prv claim against database)
        builder.Services.AddTransient<PlatformRolesVersionFilter>();

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

        // Rate limiting middleware
        app.UseRateLimiter();

        // Global exception handler with custom handling for FluentValidation
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;

                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                if (exception is ValidationException validationException)
                {
                    logger.LogWarning(validationException, "Validation failed");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/problem+json";

                    var errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());

                    var problemDetails = new HttpValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation failed",
                        Detail = "One or more validation errors occurred.",
                        Instance = context.Request.Path
                    };

                    await context.Response.WriteAsJsonAsync(problemDetails);
                    return;
                }

                // Log unhandled exceptions
                logger.LogError(exception, "Unhandled exception occurred");

                // Default error response
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    title = "An error occurred while processing your request.",
                    status = StatusCodes.Status500InternalServerError,
                    traceId = context.TraceIdentifier
                });
            });
        });

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
        app.MapHomeEndpoints();
        app.MapRoomEndpoints();
        app.MapDeviceEndpoints();
        app.MapPlatformEndpoints();
        app.MapMemberEndpoints();

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

    private static async Task RunBootstrapSuperAdminAsync(string[] args)
    {
        // Parse arguments
        string? email = null;
        string? fullName = null;
        string? password = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--email" when i + 1 < args.Length:
                    email = args[++i];
                    break;
                case "--name" when i + 1 < args.Length:
                    fullName = args[++i];
                    break;
                case "--password" when i + 1 < args.Length:
                    password = args[++i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            Console.WriteLine("Usage: dotnet run -- bootstrap-superadmin --email <email> --name <name> [--password <password>]");
            Console.WriteLine("If --password is not provided, a secure random password will be generated and displayed.");
            Environment.Exit(1);
        }

        // Build minimal host for DI
        var builder = WebApplication.CreateBuilder(new string[0]);
        builder.Configuration.AddEnvironmentVariables(prefix: "CONNECTIONSTRINGS__");

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dbContext = serviceProvider.GetRequiredService<NexusHomeDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        var platformRoleRepository = serviceProvider.GetRequiredService<IPlatformRoleRepository>();
        var userPlatformRoleRepository = serviceProvider.GetRequiredService<IUserPlatformRoleRepository>();
        var auditLogRepository = serviceProvider.GetRequiredService<IPlatformAuditLogRepository>();

        // Ensure database is created
        await dbContext.Database.EnsureCreatedAsync();

        // Check if SuperAdmin already exists
        var existingSuperAdmins = await userPlatformRoleRepository.GetByRoleKeyAsync("SuperAdmin");
        if (existingSuperAdmins.Count > 0)
        {
            Console.WriteLine("SuperAdmin already exists. Bootstrap aborted.");
            Environment.Exit(1);
        }

        // Check if user already exists
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var existingUser = await userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        if (existingUser != null)
        {
            Console.WriteLine($"User with email {email} already exists. Bootstrap aborted.");
            Environment.Exit(1);
        }

        // Generate password if not provided
        if (string.IsNullOrWhiteSpace(password))
        {
            password = GenerateSecurePassword();
            Console.WriteLine($"Generated password: {password}");
            Console.WriteLine("SAVE THIS PASSWORD - IT WILL NOT BE SHOWN AGAIN!");
        }

        // Hash password
        var passwordHash = passwordHasher.HashPassword(password);

        // Create user
        var user = new User(Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash, fullName.Trim());
        user.VerifyEmail(); // SuperAdmin is pre-verified

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Get SuperAdmin role
        var superAdminRole = await platformRoleRepository.GetByKeyAsync("SuperAdmin");
        if (superAdminRole == null)
        {
            Console.WriteLine("SuperAdmin role not found in database. Bootstrap aborted.");
            Environment.Exit(1);
        }

        // Assign SuperAdmin role
        var userPlatformRole = UserPlatformRole.Create(user.Id, superAdminRole.Id, user.Id);
        await userPlatformRoleRepository.AddAsync(userPlatformRole);

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            user.Id,
            "SuperAdminBootstrap",
            "User",
            user.Id,
            $"{{\"email\":\"{user.Email}\",\"fullName\":\"{user.FullName}\"}}");

        await auditLogRepository.AddAsync(auditLog);

        Console.WriteLine($"SuperAdmin created successfully!");
        Console.WriteLine($"User ID: {user.Id}");
        Console.WriteLine($"Email: {user.Email}");
        Console.WriteLine($"Name: {user.FullName}");
    }

    private static string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        using var random = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[16];
        random.GetBytes(bytes);
        var result = new char[16];
        for (int i = 0; i < 16; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
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