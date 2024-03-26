using DurableOrchestrator.AzureDocumentIntelligence;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(DocumentIntelligenceToMarkdownWorkflow))]
public class DocumentIntelligenceToMarkdownWorkflow()
    : BaseWorkflow(nameof(SplitPdfWorkflow))
{
    private const string OrchestrationName = "DocumentIntelligenceToMarkdownWorkflow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var workFlowInput = context.GetInput<WorkFlowInput>() ??
                            throw new ArgumentNullException(nameof(context), $"{nameof(WorkFlowInput)} is null.");

        using var span = StartActiveSpan(OrchestrationName, workFlowInput);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = workFlowInput.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(IWorkflowRequest.Validate),
                $"{nameof(workFlowInput)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(IWorkflowRequest.Validate), $"{nameof(workFlowInput)} is valid.");

        // step 3: read source file into buffer, assuming the file to read exists in the SourceBlobStorageInfo
        var sourceFile = await CallActivityAsync<byte[]?>(
            context,
            nameof(BlobStorageActivities.GetBlobContentAsBuffer),
            workFlowInput.SourceBlobStorageInfo!,
            span.Context);

        if (sourceFile == null)
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.GetBlobContentAsBuffer),
                $"{nameof(sourceFile)} is null or empty.",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to missing source file
        }

        orchestrationResults.Add(
            nameof(BlobStorageActivities.GetBlobContentAsBuffer),
            $"{nameof(sourceFile)} read into buffer.");

        // step 4: call DI layout to markdown activity
        var request = new DocumentIntelligenceRequest
        {
            Content = sourceFile,
            ValueBy = DocumentIntelligenceRequestContentType.InMemory,
            ModelId = "prebuilt-layout"
        };

        var markdown = await CallActivityAsync<byte[]?>(
            context,
            nameof(DocumentIntelligenceActivities.AnalyzeDocumentToMarkdown),
            request,
            span.Context);

        if (markdown == null)
        {
            orchestrationResults.Add(
                nameof(DocumentIntelligenceActivities.AnalyzeDocumentToMarkdown),
                $"{nameof(sourceFile)} failed to convert to markdown.",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to failed conversion
        }

        // step 5: save the markdown file to blob storage
        workFlowInput.TargetBlobStorageInfo!.Buffer = markdown;

        try
        {
            await CallActivityAsync(
                context,
                nameof(BlobStorageActivities.WriteBufferToBlob),
                workFlowInput.TargetBlobStorageInfo,
                span.Context);

            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteBufferToBlob),
                $"{nameof(markdown)} file saved to blob storage.");
        }
        catch (Exception ex)
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteBufferToBlob),
                $"{nameof(markdown)} file failed to save to blob storage. {ex.Message}",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to failed saving
        }

        return orchestrationResults.Results;
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

        var instanceId = await StartWorkflowAsync(
            starter,
            OrchestrationName,
            ExtractInput<WorkFlowInput>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
