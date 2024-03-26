using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using DurableOrchestrator.Models;
using iText.Kernel.Pdf;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(SplitPdfWorkflow))]
public class SplitPdfWorkflow()
    : BaseWorkflow(nameof(SplitPdfWorkflow))
{
    private const string OrchestrationName = "SplitPdfWorkflow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    /// <summary>
    /// Orchestrates the split of a pdf file into individual pages and saves the split pages to blob storage.
    /// </summary>
    /// <param name="context">The orchestration context providing access to workflow-related methods and properties.</param>
    /// <returns>A list of strings representing the orchestration results, which could include validation errors, informational messages, or the success message indicating the completion of sentiment analysis and data persistence.</returns>
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

        // step 3: read the source file from blob storage using the input and split the PDF file into individual pages
        var splitResults = new List<byte[]>();

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
            return orchestrationResults.Results; // Exit the orchestration due to missing blob content
        }

        try
        {
            using var pdfReader = new PdfReader(new MemoryStream(sourceFile));
            using var pdfDocument = new PdfDocument(pdfReader);

            var numberOfPages = pdfDocument.GetNumberOfPages();

            for (var i = 1; i <= numberOfPages; i++)
            {
                using var writeMemoryStream = new MemoryStream();
                await using var pdfWriter = new PdfWriter(writeMemoryStream);
                using var pdfDest = new PdfDocument(pdfWriter);

                pdfDocument.CopyPagesTo(i, i, pdfDest);
                pdfDest.Close(); // Ensure closure to flush content to stream

                splitResults.Add(writeMemoryStream.ToArray());
            }

            orchestrationResults.Add(
                nameof(PdfDocument.CopyPagesTo),
                $"{nameof(sourceFile)} was split into {numberOfPages} pages.");
        }
        catch (Exception ex)
        {
            orchestrationResults.Add(
                nameof(PdfDocument.CopyPagesTo),
                $"{nameof(sourceFile)} PDF processing failed. {ex.Message}",
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        // step 4: write the split PDF files to blob storage
        var writeTasks = new List<Task>();

        for (var i = 0; i < splitResults.Count; i++)
        {
            var blobStorageRequest = new BlobStorageRequest
            {
                StorageAccountName = workFlowInput.TargetBlobStorageInfo!.StorageAccountName,
                ContainerName = workFlowInput.TargetBlobStorageInfo!.ContainerName,
                BlobName = $"{workFlowInput.TargetBlobStorageInfo!.BlobName}_{i + 1}.pdf",
                Buffer = splitResults[i]
            };

            var writeTask = CallActivityAsync(
                context,
                nameof(BlobStorageActivities.WriteBufferToBlob),
                blobStorageRequest,
                span.Context);
            writeTasks.Add(writeTask);

            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteBufferToBlob),
                $"{blobStorageRequest.BlobName} added to write tasks.");
        }

        // Fan-out: start all write operations concurrently and wait for all of them to complete
        await Task.WhenAll(writeTasks);

        orchestrationResults.Add(
            nameof(BlobStorageActivities.WriteBufferToBlob),
            $"{nameof(splitResults)} saved to blob storage.");

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
