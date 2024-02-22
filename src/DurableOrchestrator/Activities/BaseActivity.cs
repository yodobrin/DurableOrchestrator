using DurableOrchestrator.Models;
using DurableOrchestrator.Observability;

namespace DurableOrchestrator.Activities;

/// <summary>
/// Defines the base class for all activity classes.
/// </summary>
public abstract class BaseActivity(string activityName, ObservabilitySettings observabilitySettings)
{
    protected readonly TracerProvider ActivityTracerProvider = Sdk.CreateTracerProviderBuilder().ConfigureTracerBuilder(activityName, observabilitySettings).Build();

    protected TelemetrySpan StartActiveSpan(string name, IObservableContext? input = default)
    {
        var tracer = ActivityTracerProvider.GetTracer(activityName);
        return input != default ? tracer.StartActiveSpan(name, SpanKind.Internal, ExtractTracingContext(input)) : tracer.StartActiveSpan(name);
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
