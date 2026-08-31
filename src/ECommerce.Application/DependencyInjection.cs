using ECommerce.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application;

/// <summary>
/// Provides dependency injection registration for the Application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(
                typeof(DependencyInjection).Assembly);

            // Order matters: MediatR runs behaviors in registration order, so a request
            // is logged (including invalid ones) before ValidationBehavior can reject it.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        return services;
    }
}
