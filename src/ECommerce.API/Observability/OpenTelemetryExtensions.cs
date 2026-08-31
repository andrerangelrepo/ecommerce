using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ECommerce.API.Observability;

/// <summary>
/// Provides dependency injection registration for OpenTelemetry tracing and metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers and configures OpenTelemetry tracing and metrics for the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>OpenTelemetry</c> configuration section is missing or invalid.
    /// </exception>
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(OpenTelemetryOptions.SectionName);
        var options = section.Get<OpenTelemetryOptions>()
            ?? throw new InvalidOperationException("OpenTelemetry configuration is invalid.");

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                options,
                new ValidationContext(options),
                validationResults,
                validateAllProperties: true))
        {
            var errors = string.Join("; ", validationResults.Select(result => result.ErrorMessage));
            throw new InvalidOperationException($"Invalid OpenTelemetry configuration: {errors}");
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddSource(ApplicationTelemetry.ActivitySourceName);

                if (options.ConsoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }

                if (options.Otlp.Enabled)
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.Otlp.Endpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.MetricsConsoleExporterEnabled)
                {
                    // Metrics export on a fixed timer regardless of traffic — the default
                    // interval floods the console with periodic dumps even when idle, so
                    // it's widened here whenever this (opt-in) exporter is turned on.
                    metrics.AddConsoleExporter((_, readerOptions) =>
                        readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 30_000);
                }

                if (options.Otlp.Enabled)
                {
                    metrics.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.Otlp.Endpoint));
                }
            });

        return services;
    }
}
