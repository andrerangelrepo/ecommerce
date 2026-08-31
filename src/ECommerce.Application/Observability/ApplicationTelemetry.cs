using System.Diagnostics;

namespace ECommerce.Application.Observability;

/// <summary>
/// Exposes the <see cref="System.Diagnostics.ActivitySource"/> used to trace Application-layer
/// work. Depends only on the BCL (<see cref="System.Diagnostics"/>) — the OpenTelemetry SDK
/// that actually collects and exports these activities is wired up in the API layer.
/// </summary>
public static class ApplicationTelemetry
{
    /// <summary>The name under which the API registers this source with OpenTelemetry.</summary>
    public const string ActivitySourceName = "ECommerce.Application";

    /// <summary>The source used to start Activities for Commands and Queries.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
