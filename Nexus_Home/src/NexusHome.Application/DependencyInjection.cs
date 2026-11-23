namespace NexusHome.Application;

using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Common.Behaviors;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Shared authorization services
        services.AddScoped<HomeAuthorizationService>();

        return services;
    }
}