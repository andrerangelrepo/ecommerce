using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Observability;

/// <summary>
/// Represents the settings used to configure OpenTelemetry tracing and metrics.
/// </summary>
public sealed class OpenTelemetryOptions
{
    /// <summary>
    /// The configuration section containing the OpenTelemetry settings.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Gets the service name reported as <c>service.name</c> on every trace/metric.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether traces/metrics are also written to the console — useful for local
    /// development and demonstration; not intended as a production export strategy.
    /// </summary>
    public bool ConsoleExporterEnabled { get; init; }

    /// <summary>
    /// Gets the optional OTLP exporter settings, disabled by default so the application
    /// never depends on a collector being reachable.
    /// </summary>
    public OtlpOptions Otlp { get; init; } = new();
}

/// <summary>
/// Represents the settings used to export telemetry to an OTLP collector.
/// </summary>
public sealed class OtlpOptions
{
    /// <summary>Gets whether the OTLP exporter is registered.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the OTLP collector endpoint, used only when <see cref="Enabled"/> is <see langword="true"/>.</summary>
    public string Endpoint { get; init; } = string.Empty;
}
