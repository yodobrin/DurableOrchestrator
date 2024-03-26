using DurableOrchestrator.AzureDocumentIntelligence;
using DurableOrchestrator.Models;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(DILayout2mdWorkFlow))]
public class DILayout2mdWorkFlow()
    : BaseWorkflow(nameof(SplitPdfWorkflow))
{
    private const string OrchestrationName = "DILayout2mdWorkFlow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

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
            orchestrationResults.AddRange(validationResult.ValidationMessages); // some of the 'errors' are not really errors, but just informational messages
            log.LogError($"{OrchestrationName}::WorkFlowInput is invalid. {validationResult}");
            return orchestrationResults; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add($"{OrchestrationName}::WorkFlowInput is valid.");
        log.LogInformation($"{OrchestrationName}::WorkFlowInput is valid.");
        // step 3: read source file into buffer, assuming the file to read exists in the SourceBlobStorageInfo
        var sourceBlobStorageInfo = workFlowInput.SourceBlobStorageInfo!;
        sourceBlobStorageInfo.InjectObservabilityContext(span.Context);

        var sourceFile = await context.CallActivityAsync<byte[]>(nameof(BlobStorageActivities.GetBlobContentAsBuffer), sourceBlobStorageInfo);
        if (sourceFile == null)
        {
            log.LogError($"{OrchestrationName}::Source file is null or empty.");
            orchestrationResults.Add("Source file is null or empty.");
            return orchestrationResults; // Exit the orchestration due to missing source file
        }
        orchestrationResults.Add($"{OrchestrationName}::Read input file.");
        // step 4: call DI layout to markdown activity
        DocumentIntelligenceRequest request = new DocumentIntelligenceRequest
        {
            Content = sourceFile,
            ValueBy = DocumentIntelligenceRequestContentType.InMemory,
            ModelId = "prebuilt-layout"
        };
        var markDown = await context.CallActivityAsync<byte[]>(nameof(DocumentIntelligenceActivities.AnalyzeDocumentToMarkdown), request);
        if(markDown == null)
        {
            log.LogError($"{OrchestrationName}::Failed to convert layout to markdown.");
            orchestrationResults.Add("Failed to convert layout to markdown.");
            return orchestrationResults; // Exit the orchestration due to failed conversion
        }
        // step 5: save the markdown file to blob storage
        var targetBlobStorageInfo = workFlowInput.TargetBlobStorageInfo!;
        targetBlobStorageInfo.InjectObservabilityContext(span.Context);
        targetBlobStorageInfo.Buffer = markDown;

        try{
            await context.CallActivityAsync(nameof(BlobStorageActivities.WriteBufferToBlob), targetBlobStorageInfo);
            orchestrationResults.Add($"{OrchestrationName}::Successfully saved markdown file to blob storage.");
        }catch(Exception ex)
        {
            log.LogError($"{OrchestrationName}::Failed to save markdown file to blob storage. {ex.Message}");
            orchestrationResults.Add("Failed to save markdown file to blob storage.");
            return orchestrationResults; // Exit the orchestration due to failed saving
        }

        return orchestrationResults;
    }

    /// <summary>
    /// HTTP-triggered function that starts the text analytics orchestration. It extracts input from the HTTP request body, schedules a new orchestration instance for sentiment analysis, and returns a response with the status check URL.
    /// </summary>
    /// <param name="req">The HTTP request containing the input for the text analytics workflow.</param>
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

        // Function input extracted from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestrationName, input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
