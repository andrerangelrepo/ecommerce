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
    /// Gets whether traces are also written to the console — useful for local development
    /// and demonstration; not intended as a production export strategy. Defaults to
    /// <see langword="false"/>: the raw span dump (~20 lines each, most of it identical
    /// boilerplate — Resource/SDK metadata repeated on every span) drowns out Serilog's
    /// curated log lines for day-to-day use. Enable it when inspecting span hierarchy.
    /// </summary>
    public bool ConsoleExporterEnabled { get; init; }

    /// <summary>
    /// Gets whether metrics are also written to the console. Defaults to <see langword="false"/>:
    /// unlike traces, metrics export on a fixed timer regardless of traffic, so leaving this on
    /// floods the console with periodic dumps even when the API is idle. Enable it only when
    /// actively inspecting metrics locally.
    /// </summary>
    public bool MetricsConsoleExporterEnabled { get; init; }

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
