namespace NexusHome.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusHome.Application.Interfaces;
using NexusHome.Infrastructure.Authentication;
using NexusHome.Infrastructure.Data;
using NexusHome.Infrastructure.Repositories;
using Npgsql;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Enable dynamic JSON serialization for Dictionary<string, object> to JSONB columns
        // This must be called before any DbContext is created
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        var connectionString = configuration.GetConnectionString("NexusHome")
            ?? throw new InvalidOperationException("Connection string 'NexusHome' not found.");

        services.AddDbContext<NexusHomeDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(NexusHomeDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Enable detailed errors in development (but NOT sensitive data logging)
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableDetailedErrors();
                // Sensitive data logging disabled by default - enable only when debugging data issues
                // options.EnableSensitiveDataLogging();
            }
        });

        // Register JWT settings
        services.Configure<JwtSettings>(options => configuration.GetSection("Jwt").Bind(options));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

        // Register claim credential settings
        services.Configure<ClaimCredentialSettings>(options => configuration.GetSection("ClaimCredential").Bind(options));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClaimCredentialSettings>>().Value);

        // Register authentication services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IHomeRepository, HomeRepository>();
        services.AddScoped<IClaimCredentialRepository, ClaimCredentialRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IUserPlatformRoleRepository, UserPlatformRoleRepository>();
        services.AddScoped<IPlatformAuditLogRepository, PlatformAuditLogRepository>();

        return services;
    }
}