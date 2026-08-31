using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ECommerce.API.Observability;

/// <summary>
/// Adds the current <see cref="Activity"/>'s <c>TraceId</c>/<c>SpanId</c> to every log event,
/// reading <see cref="Activity.Current"/> directly rather than adding a separate correlation
/// package — the same ambient context that OpenTelemetry's instrumentation already populates.
/// </summary>
internal sealed class ActivityEnricher : ILogEventEnricher
{
    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;

        if (activity is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}
