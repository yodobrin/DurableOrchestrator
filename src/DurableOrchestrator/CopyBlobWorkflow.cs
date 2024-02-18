using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator;

[ActivitySource(nameof(CopyBlobWorkflow))]
public class CopyBlobWorkflow
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(CopyBlobWorkflow));
    private const string OrchestrationName = "CopyBlobWorkflow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";
    [Function("CopyBlobWorkflow")]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        using var span = _tracer.StartActiveSpan(OrchestrationName);

        var log = context.CreateReplaySafeLogger(OrchestrationName);
        var orchestrationResults = new List<string>();
        // step 1: obtain input for the workflow
 
        var workFlowInput = context.GetInput<WorkFlowInput>();
        // step 2: validate the input
        if (workFlowInput == null)
        {
            log.LogError("WorkFlowInput is null.");
            orchestrationResults.Add("Workflow input was not provided.");
            return orchestrationResults; // Exit the orchestration due to missing WorkFlowInput
        }
        if (!ValidateWorkFlowInput(workFlowInput, log))
        {
            orchestrationResults.Add("Missing required details in WorkFlowInput or BlobStorageInfo.");
            return orchestrationResults; // Exit the orchestration due to missing required details
        }
        // step 3: get blob content to be copied
        var blobContent = await context.CallActivityAsync<byte[]>("GetBlobContentAsBuffer", workFlowInput.SourceBlobStorageInfo);
        log.LogInformation($"Retrieved blob content size: {blobContent?.Length ?? 0} bytes.");        

        // step 4: write to another blob
        workFlowInput.SourceBlobStorageInfo!.Buffer = blobContent;
        await context.CallActivityAsync<string>("WriteBufferToBlob",workFlowInput.TargetBlobStorageInfo);
        return orchestrationResults;
    }

    static bool ValidateWorkFlowInput(WorkFlowInput workFlowInput, ILogger log)
    {
        if (string.IsNullOrEmpty(workFlowInput.Name) || workFlowInput.SourceBlobStorageInfo == null ||
            string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.BlobName) || 
            string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.ContainerName))
        {
            log.LogError("Missing required details in WorkFlowInput or BlobStorageInfo.");
            return false;
        }
        return true;
    }

    [Function(OrchestrationTriggerName)]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient starter,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(OrchestrationTriggerName);

        var log = executionContext.GetLogger(OrchestrationTriggerName);

        var requestBody = await req.ReadAsStringAsync();
        // Check for an empty request body as a more direct approach
        if (string.IsNullOrEmpty(requestBody))
        {
             throw new ArgumentException("The request body must not be null or empty.", nameof(req));
        } 

        var input = JsonSerializer.Deserialize<WorkFlowInput>(requestBody) ??
                    throw new ArgumentException("The request body is not a valid WorkFlowInput.", nameof(req));
        // Function input comes from the request content.
        string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("CopyBlobWorkflow", input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}