using DurableOrchestrator.Models;

namespace DurableOrchestrator.Activities;

/// <summary>
/// Defines the base class for all activity classes.
/// </summary>
public abstract class BaseActivity(string activityName)
{
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(activityName);

    protected TelemetrySpan StartActiveSpan(string name, IObservableContext? input = default)
    {
        return input != default ? Tracer.StartActiveSpan(name, SpanKind.Internal, ExtractTracingContext(input)) : Tracer.StartActiveSpan(name);
    }

    protected void InjectTracingContext(IObservableContext activityInput, SpanContext spanContext)
    {
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(spanContext, Baggage.Current),
            activityInput.ObservableProperties,
            (props, key, value) =>
            {
                props ??= new Dictionary<string, object>();
                props.TryAdd(key, value);
            });
    }

    protected SpanContext ExtractTracingContext(IObservableContext activityInput)
    {
        var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            activityInput.ObservableProperties,
            (props, key) =>
            {
                if (!props.TryGetValue(key, out var value) || value.ToString() is null)
                {
                    return [];
                }

                return [value.ToString()];
            });

        return new SpanContext(propagationContext.ActivityContext);
    }
}
