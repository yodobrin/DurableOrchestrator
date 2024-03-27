using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;

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
        var input = context.GetInput<CopyBlobWorkflowRequest>() ??
                            throw new ArgumentNullException(nameof(context), $"{nameof(CopyBlobWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(CopyBlobWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(CopyBlobWorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3: get blob content to be copied
        var blobContent = await CallActivityAsync<byte[]?>(
            context,
            nameof(BlobStorageActivities.GetBlobContentAsBuffer),
            input.SourceBlobStorageInfo!,
            span.Context);

        if (blobContent == null || blobContent.Length == 0)
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.GetBlobContentAsBuffer),
                $"{nameof(blobContent)} is empty or null.",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to missing blob content
        }

        // step 4: write to another blob
        input.TargetBlobStorageInfo!.Buffer = blobContent;

        try
        {
            await CallActivityAsync<string>(
                context,
                nameof(BlobStorageActivities.WriteBufferToBlob),
                input.TargetBlobStorageInfo,
                span.Context);

            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteBufferToBlob),
                $"{nameof(blobContent)} file saved to blob storage.");
        }
        catch (Exception ex)
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteBufferToBlob),
                $"{nameof(blobContent)} file failed to save to blob storage. {ex.Message}",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to an error during the write operation
        }

        return orchestrationResults.Results;
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

        var instanceId = await StartWorkflowAsync(
            starter,
            OrchestrationName,
            ExtractInput<CopyBlobWorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }

    public class CopyBlobWorkflowRequest : WorkflowRequestBase
    {
        [JsonPropertyName("sourceBlobStorageInfo")]
        public BlobStorageRequest? SourceBlobStorageInfo { get; set; }

        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.Merge(ValidateBlobStorageInfo(TargetBlobStorageInfo, "Target"));
            result.Merge(ValidateBlobStorageInfo(SourceBlobStorageInfo, "Source"));
            return result;
        }
    }
}
