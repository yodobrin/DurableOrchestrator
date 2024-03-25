using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(CopyBlobWorkflow))]
public class CopyBlobWorkflow()
    : BaseWorkflow(nameof(CopyBlobWorkflow))
{
    private const string OrchestrationName = "CopyBlobWorkflow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    /// <summary>
    /// Orchestrates the process of copying content from a source blob to a target blob. It involves validating the workflow input, retrieving the content of the source blob, and writing that content to the target blob.
    /// </summary>
    /// <param name="context">The orchestration context providing access to workflow-related methods and properties.</param>
    /// <returns>A list of strings representing the orchestration results, which could include validation errors, informational messages, or a success message indicating the completion of the copy operation.</returns>
    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var workFlowInput = context.GetInput<WorkFlowInput>() ??
                            throw new ArgumentNullException(nameof(context), "WorkFlowInput is null.");

        using var span = StartActiveSpan(OrchestrationName, workFlowInput);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new List<string>();

        // step 2: validate the input
        var validationResult = workFlowInput.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(validationResult.ValidationMessages);
            log.LogError($"CopyBlobWorkflow::WorkFlowInput is invalid. {validationResult}");
            return orchestrationResults; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add("CopyBlobWorkflow::WorkFlowInput is valid.");
        log.LogInformation("CopyBlobWorkflow::WorkFlowInput is valid.");

        // step 3: get blob content to be copied
        var sourceBlobStorageInfo = workFlowInput.SourceBlobStorageInfo!;
        sourceBlobStorageInfo.InjectObservabilityContext(span.Context);

        var blobContent = await context.CallActivityAsync<byte[]>(nameof(BlobStorageActivities.GetBlobContentAsBuffer),
            sourceBlobStorageInfo);
        log.LogInformation($"CopyBlobWorkflow::Retrieved blob content size: {blobContent?.Length ?? 0} bytes.");

        if (blobContent == null || blobContent.Length == 0)
        {
            log.LogError("CopyBlobWorkflow::Blob content is empty or null.");
            orchestrationResults.Add("Blob content is empty or null.");
            return orchestrationResults; // Exit the orchestration due to missing blob content
        }

        // step 4: write to another blob
        var targetBlobStorageInfo = workFlowInput.TargetBlobStorageInfo!;
        targetBlobStorageInfo.Buffer = blobContent;
        targetBlobStorageInfo.InjectObservabilityContext(span.Context);

        await context.CallActivityAsync<string>(nameof(BlobStorageActivities.WriteBufferToBlob), targetBlobStorageInfo);
        return orchestrationResults;
    }

    /// <summary>
    /// HTTP-triggered function that starts the blob copy orchestration. It extracts input from the HTTP request body, schedules a new orchestration instance for the copy operation, and returns a response with the status check URL.
    /// </summary>
    /// <param name="req">The HTTP request containing the input for the copy blob workflow.</param>
    /// <param name="starter">The durable task client used to schedule new orchestration instances.</param>
    /// <param name="executionContext">The function execution context for logging and other execution-related functionalities.</param>
    /// <returns>A response with the HTTP status code and the URL to check the orchestration status.</returns>
    [Function(OrchestrationTriggerName)]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient starter,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(OrchestrationTriggerName);

        var log = executionContext.GetLogger(OrchestrationTriggerName);

        var requestBody = await req.ReadAsStringAsync();

        // Check for an empty request body as a more direct approach
        if (string.IsNullOrEmpty(requestBody))
        {
            throw new ArgumentException("The request body must not be null or empty.", nameof(req));
        }

        var input = ExtractInput<WorkFlowInput>(requestBody);
        input.InjectObservabilityContext(span.Context);

        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestrationName, input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
