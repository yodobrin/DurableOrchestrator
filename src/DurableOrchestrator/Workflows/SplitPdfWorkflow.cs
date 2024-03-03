using iText.Kernel.Pdf;
using DurableOrchestrator.Models;
using DurableOrchestrator.Observability;
using DurableOrchestrator.Storage;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(SplitPdfWorkflow))]
public class SplitPdfWorkflow(ObservabilitySettings observabilitySettings)
    : BaseWorkflow(nameof(SplitPdfWorkflow), observabilitySettings)
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
                            throw new ArgumentNullException(nameof(context), "WorkFlowInput is null.");

        using var span = StartActiveSpan(OrchestrationName, workFlowInput);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new List<string>();

        // step 2: validate the input
        var validationResult = ValidateWorkFlowInputs(workFlowInput!);
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(validationResult
                .ValidationMessages); // some of the 'errors' are not really errors, but just informational messages
            log.LogError(
                $"SplitPdfWorkflow::WorkFlowInput is invalid. {validationResult.GetValidationMessages()}");
            return orchestrationResults; // Exit the orchestration due to validation errors
        }
        
        orchestrationResults.Add("SplitPdfWorkflow::WorkFlowInput is valid.");
        log.LogInformation("SplitPdfWorkflow::WorkFlowInput is valid.");

        // step 3:
        // read the source file from blob storage using the input
        var splitResults = new List<byte[]>();
        byte[] sourceFile = await context.CallActivityAsync<byte[]>(nameof(BlobStorageActivities.GetBlobContentAsBuffer), workFlowInput.SourceBlobStorageInfo!);

        if (sourceFile == null)
        {
            log.LogError("SplitPdfWorkflow::Source file is null or empty.");
            orchestrationResults.Add("Source file is null or empty.");
            return orchestrationResults; // Exit the orchestration due to missing source file
        }

        // split the PDF file into individual pages
        try{
            using var pdfReader = new PdfReader(new MemoryStream(sourceFile));
            using var pdfDocument = new PdfDocument(pdfReader);

            int numberOfPages = pdfDocument.GetNumberOfPages();
            orchestrationResults.Add($"SplitPdfWorkflow::Number of pages in the PDF: {numberOfPages}");
            for (int i = 1; i <= numberOfPages; i++)
            {
                using (var writeMemoryStream = new MemoryStream())
                using (var pdfWriter = new PdfWriter(writeMemoryStream))
                using (var pdfDest = new PdfDocument(pdfWriter))
                {
                    pdfDocument.CopyPagesTo(i, i, pdfDest);
                    pdfDest.Close(); // Ensure closure to flush content to stream

                    splitResults.Add(writeMemoryStream.ToArray());
                    
                }
            }

        }
        catch (iText.Kernel.Exceptions.PdfException ex)
        {
            log.LogError($"SplitPdfWorkflow::PDF processing error: {ex.Message}, stack: {ex.StackTrace}, Details: {ex.InnerException}");
            orchestrationResults.Add($"PDF processing error: {ex.Message}");
             return orchestrationResults;
        }
        catch (Exception ex)
        {
            log.LogError($"SplitPdfWorkflow::Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
            orchestrationResults.Add($"Unexpected error: {ex.Message}");
             return orchestrationResults;
        }
        // step 4: write the split PDF files to blob storage
        var writeTasks = new List<Task>();
        for (int i = 0; i < splitResults.Count; i++)
        {
            var blobStorageInfo = new BlobStorageInfo
            {
                StorageAccountName = workFlowInput.TargetBlobStorageInfo!.StorageAccountName,
                ContainerName = workFlowInput.TargetBlobStorageInfo!.ContainerName,
                BlobName = $"{workFlowInput.TargetBlobStorageInfo!.BlobName}_{i + 1}.pdf",
                Buffer = splitResults[i]
            };
            var task = context.CallActivityAsync(nameof(BlobStorageActivities.WriteBufferToBlob), blobStorageInfo);
            orchestrationResults.Add($"SplitPdfWorkflow:: Added split pdf: {blobStorageInfo.BlobName} to the write tasks.");
            writeTasks.Add(task);
        }
        // Fan-out: start all write operations concurrently and wait for all of them to complete
        await Task.WhenAll(writeTasks);
    

        orchestrationResults.Add("SplitPdfWorkflow::Split completed.");

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

        var input = ExtractInput(requestBody);
        InjectTracingContext(input, span.Context);

        
        // Function input extracted from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestrationName, input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
