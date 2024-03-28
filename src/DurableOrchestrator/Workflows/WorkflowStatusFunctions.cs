using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Workflows;

/// <summary>
/// Defines a collection of functions for observing the status of a workflow instance.
/// </summary>
[ActivitySource]
public class WorkflowStatusFunctions
{
    /// <summary>
    /// Retrieves the status of a workflow instance.
    /// </summary>
    /// <remarks>
    /// This is useful for checking the status of a workflow that is not HTTP-triggered.
    /// </remarks>
    /// <param name="req">The HTTP request.</param>
    /// <param name="instanceId">The ID of the workflow instance.</param>
    /// <param name="starter">The durable task client used to query the status of the workflow instance.</param>
    /// <returns>A <see cref="OrchestrationMetadata"/> object representing the status of the workflow instance.</returns>
    [Function(nameof(GetWorkflowStatus))]
    public async Task<OrchestrationMetadata?> GetWorkflowStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "workflow/{instanceId}")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient starter)
    {
        return await starter.GetInstanceAsync(instanceId);
    }
}
