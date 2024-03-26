using System.Text.Json;
using DurableOrchestrator.Core.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.Core;

/// <summary>
/// Defines a base class for all workflow classes.
/// </summary>
/// <param name="name">The name of the workflow used for observability.</param>
[ActivitySource(nameof(BaseWorkflow))]
public abstract class BaseWorkflow(string name)
{
    /// <summary>
    /// Defines the tracer for the workflow.
    /// </summary>
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(name);

    /// <summary>
    /// Extracts the input for the workflow from the request body.
    /// </summary>
    /// <typeparam name="TInput">The type of <see cref="IWorkflowRequest"/> to extract.</typeparam>
    /// <param name="requestBody">The request body to extract the input from.</param>
    /// <returns>The extracted input for the workflow.</returns>
    /// <exception cref="ArgumentException">Thrown when the request body is not a valid JSON representation of a <see cref="IWorkflowRequest"/> object.</exception>
    protected static TInput ExtractInput<TInput>(string requestBody)
        where TInput : class, IWorkflowRequest
    {
        return JsonSerializer.Deserialize<TInput>(requestBody) ??
               throw new ArgumentException(
                   $"The request body is not a valid JSON representation of a {nameof(IWorkflowRequest)} object.",
                   nameof(requestBody));
    }

    /// <summary>
    /// Starts a span for the workflow and makes it the active span.
    /// </summary>
    /// <param name="name">The name of the workflow span.</param>
    /// <param name="context">The observability context for the workflow.</param>
    /// <returns>A new active span for the workflow.</returns>
    protected TelemetrySpan StartActiveSpan(string name, IObservabilityContext? context = default)
    {
        return context != default
            ? Tracer.StartActiveSpan(name, SpanKind.Internal, context.ExtractObservabilityContext())
            : Tracer.StartActiveSpan(name);
    }
}
