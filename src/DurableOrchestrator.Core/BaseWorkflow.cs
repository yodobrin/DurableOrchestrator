using System.Text.Json;
using DurableOrchestrator.Core.Observability;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.Core;

/// <summary>
/// Defines a base class for all workflow classes.
/// </summary>
/// <param name="name">The name of the workflow used for observability.</param>
[ActivitySource]
public abstract class BaseWorkflow(string name)
{
    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    protected string Name { get; } = name;

    /// <summary>
    /// Defines the tracer for the workflow.
    /// </summary>
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(name);

    /// <summary>
    /// Starts a new workflow instance from the request of a durable function.
    /// </summary>
    /// <param name="durableFunctionClient">The durable function client used to start the workflow.</param>
    /// <param name="input">The input for the workflow.</param>
    /// <param name="spanContext">The parent span context for the workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the ID of the started workflow instance.</returns>
    protected Task<string> StartWorkflowAsync(
        DurableTaskClient durableFunctionClient,
        IWorkflowRequest input,
        SpanContext spanContext = default,
        CancellationToken cancellationToken = default)
    {
        if (spanContext != default)
        {
            input.InjectObservabilityContext(spanContext);
        }

        return durableFunctionClient.ScheduleNewOrchestrationInstanceAsync(Name, input, cancellation: cancellationToken);
    }

    /// <summary>
    /// Calls a sub-workflow from the current workflow.
    /// </summary>
    /// <typeparam name="T">The type of the result from the sub-workflow.</typeparam>
    /// <param name="workflowContext">The current workflow context.</param>
    /// <param name="subWorkflowName">The name of the sub-workflow to call.</param>
    /// <param name="input">The input for the sub-workflow.</param>
    /// <param name="spanContext">The parent span context for the sub-workflow.</param>
    /// <returns>A task representing the asynchronous operation that returns the result from the sub-workflow.</returns>
    protected Task<T> CallWorkflowAsync<T>(
        TaskOrchestrationContext workflowContext,
        string subWorkflowName,
        IWorkflowRequest input,
        SpanContext spanContext = default)
    {
        if (spanContext != default)
        {
            input.InjectObservabilityContext(spanContext);
        }

        return workflowContext.CallSubOrchestratorAsync<T>(subWorkflowName, input);
    }

    /// <summary>
    /// Calls an activity from the current workflow.
    /// </summary>
    /// <param name="workflowContext">The current workflow context.</param>
    /// <param name="activityName">The name of the activity to call. Use the nameof operator to get the name of the activity.</param>
    /// <param name="input">The input for the activity.</param>
    /// <param name="spanContext">The parent span context for the activity.</param>
    /// <returns>A task representing the asynchronous operation that returns the result from the activity.</returns>
    protected Task CallActivityAsync(
        TaskOrchestrationContext workflowContext,
        string activityName,
        IWorkflowRequest input,
        SpanContext spanContext = default)
    {
        if (spanContext != default)
        {
            input.InjectObservabilityContext(spanContext);
        }

        return workflowContext.CallActivityAsync(activityName, input);
    }

    /// <summary>
    /// Calls an activity from the current workflow.
    /// </summary>
    /// <typeparam name="T">The type of the result from the activity.</typeparam>
    /// <param name="workflowContext">The current workflow context.</param>
    /// <param name="activityName">The name of the activity to call. Use the nameof operator to get the name of the activity.</param>
    /// <param name="input">The input for the activity.</param>
    /// <param name="spanContext">The parent span context for the activity.</param>
    /// <returns>A task representing the asynchronous operation that returns the result from the activity.</returns>
    protected Task<T> CallActivityAsync<T>(
        TaskOrchestrationContext workflowContext,
        string activityName,
        IWorkflowRequest input,
        SpanContext spanContext = default)
    {
        if (spanContext != default)
        {
            input.InjectObservabilityContext(spanContext);
        }

        return workflowContext.CallActivityAsync<T>(activityName, input);
    }

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
    /// <param name="spanName">The name of the workflow span.</param>
    /// <param name="context">The observability context for the workflow.</param>
    /// <returns>A new active span for the workflow.</returns>
    protected TelemetrySpan StartActiveSpan(string spanName, IObservabilityContext? context = default)
    {
        return context != default
            ? Tracer.StartActiveSpan(spanName, SpanKind.Internal, context.ExtractObservabilityContext())
            : Tracer.StartActiveSpan(spanName);
    }
}
