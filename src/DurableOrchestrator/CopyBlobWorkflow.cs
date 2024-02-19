using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator;

[ActivitySource(nameof(CopyBlobWorkflow))]
public class CopyBlobWorkflow : BaseWorkflow
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(CopyBlobWorkflow));
    public CopyBlobWorkflow() : base(nameof(CopyBlobWorkflow)) { }
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
        ValidationResult validationResult = ValidateWorkFlowInputs(workFlowInput!);
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(validationResult.Errors);
            return orchestrationResults; // Exit the orchestration due to validation errors
        }
        else
        {
            orchestrationResults.Add("WorkFlowInput is valid.");
        }
        // step 3: get blob content to be copied
        var blobContent = await context.CallActivityAsync<byte[]>("GetBlobContentAsBuffer", workFlowInput!.SourceBlobStorageInfo);
        log.LogInformation($"Retrieved blob content size: {blobContent?.Length ?? 0} bytes.");

        if(blobContent == null || blobContent.Length == 0)
        {
            log.LogError("Blob content is empty or null.");
            orchestrationResults.Add("Blob content is empty or null.");
            return orchestrationResults; // Exit the orchestration due to missing blob content
        }

        // step 4: write to another blob
        workFlowInput.SourceBlobStorageInfo!.Buffer = blobContent;
        await context.CallActivityAsync<string>("WriteBufferToBlob",workFlowInput.TargetBlobStorageInfo);
        return orchestrationResults;
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
        var input = ExtractInput(requestBody);

        // Function input comes from the request content.
        string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("CopyBlobWorkflow", input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}