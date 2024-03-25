using DurableOrchestrator.Core.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.Core;

/// <summary>
/// Defines the base class for all activity classes.
/// </summary>
/// <param name="name">The name of the activity used for observability.</param>
[ActivitySource(nameof(BaseActivity))]
public abstract class BaseActivity(string name)
{
    /// <summary>
    /// Defines the tracer for the activity.
    /// </summary>
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(name);

    /// <summary>
    /// Starts a span for the activity and makes it the active span.
    /// </summary>
    /// <param name="name">The name of the activity span.</param>
    /// <param name="context">The observability context for the activity.</param>
    /// <returns>A new active span for the activity.</returns>
    protected TelemetrySpan StartActiveSpan(string name, IObservabilityContext? context = default)
    {
        return context != default
            ? Tracer.StartActiveSpan(name, SpanKind.Internal, context.ExtractObservabilityContext())
            : Tracer.StartActiveSpan(name);
    }
}
