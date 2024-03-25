using DurableOrchestrator.Models;

namespace DurableOrchestrator.Activities;

/// <summary>
/// Defines the base class for all activity classes.
/// </summary>
[ActivitySource(nameof(BaseActivity))]
public abstract class BaseActivity(string activityName)
{
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(activityName);

    protected TelemetrySpan StartActiveSpan(string name, IObservableContext? input = default)
    {
        var tracer = Tracer; //ActivityTracerProvider.GetTracer(activityName);
        return input != default ? tracer.StartActiveSpan(name, SpanKind.Internal, input.ExtractTracingContext()) : tracer.StartActiveSpan(name);
    }
}
