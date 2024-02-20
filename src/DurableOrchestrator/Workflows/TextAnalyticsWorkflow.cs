using DurableOrchestrator.Observability;
using DurableOrchestrator.Models;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(TextAnalyticsWorkflow))]
public class TextAnalyticsWorkflow : BaseWorkflow
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(TextAnalyticsWorkflow));
    public TextAnalyticsWorkflow() : base(nameof(TextAnalyticsWorkflow)) { }
    private const string OrchestrationName = "TextAnalyticsWorkflow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function("BatchExtractSentiment")]
    /// <summary>
    /// Orchestrates the sentiment analysis process by validating input, performing sentiment analysis on each text analytics request (fan-out/fan-in pattern), and saving the analysis results to blob storage.
    /// </summary>
    /// <param name="context">The orchestration context providing access to workflow-related methods and properties.</param>
    /// <returns>A list of strings representing the orchestration results, which could include validation errors, informational messages, or the success message indicating the completion of sentiment analysis and data persistence.</returns>
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
            log.LogError($"BatchExtractSentiment::WorkFlowInput is invalid. {validationResult.GetValidationMessages()}");
            orchestrationResults.AddRange(validationResult.ValidationMessages); // some of the 'errors' are not really errors, but just informational messages
            return orchestrationResults; // Exit the orchestration due to validation errors
        }
        else
        {
            orchestrationResults.Add("BatchExtractSentiment::WorkFlowInput is valid.");
        }
        // step 3: 
        // call the sentiment analysis activity for each text analytics request - fan-out/fan-in
        var sentimentTasks = new List<Task<string?>>();
        // both workflowinput and textanalyticsrequests are not null - checked above
        foreach (var request in workFlowInput!.TextAnalyticsRequests!)
        {
            // Fan-out: start a task for each text analytics request
            var task = context.CallActivityAsync<string?>(("GetSentiment"), request);
            sentimentTasks.Add(task);
        }
        // Fan-in: Wait for all sentiment analysis tasks to complete
        await Task.WhenAll(sentimentTasks);
        // Collect results
        var sentiments = new List<string?>();
        foreach (var task in sentimentTasks)
        {
            sentiments.Add(await task); // Here, you could handle nulls or errors as needed
        }

        orchestrationResults.Add("BatchExtractSentiment::Sentiment analysis completed.");
        // create a new file, and save the sentiments gathered from the text analytics together with the original text
        AnalysisResults analysisResults = new AnalysisResults();
        for (int i = 0; i < workFlowInput!.TextAnalyticsRequests!.Count; i++)
        {
            analysisResults.Results.Add(new TextSentimentResult
            {
                OriginalText = workFlowInput.TextAnalyticsRequests[i].TextsToAnalyze,
                Sentiment = sentiments[i] ?? "Error"
            });
        }

        var blobContent = JsonSerializer.Serialize(analysisResults, new JsonSerializerOptions { WriteIndented = true });

        // first define where to write the file
        var targetBlobStorageInfo = workFlowInput.TargetBlobStorageInfo!;

        if (blobContent == null || blobContent.Length == 0)
        {
            log.LogError("BatchExtractSentiment::Blob content is empty or null.");
            orchestrationResults.Add("Blob content is empty or null.");
            return orchestrationResults; // Exit the orchestration due to missing blob content
        }

        // step 4: write to another blob
        targetBlobStorageInfo.Buffer = System.Text.Encoding.UTF8.GetBytes(blobContent);
        await context.CallActivityAsync<string>("WriteBufferToBlob",targetBlobStorageInfo);
        return orchestrationResults;
    }

    [Function(OrchestrationTriggerName)]
    /// <summary>
    /// HTTP-triggered function that starts the text analytics orchestration. It extracts input from the HTTP request body, schedules a new orchestration instance for sentiment analysis, and returns a response with the status check URL.
    /// </summary>
    /// <param name="req">The HTTP request containing the input for the text analytics workflow.</param>
    /// <param name="starter">The durable task client used to schedule new orchestration instances.</param>
    /// <param name="executionContext">The function execution context for logging and other execution-related functionalities.</param>
    /// <returns>A response with the HTTP status code and the URL to check the orchestration status.</returns>
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

        // Function input extracted from the request content.
        string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("BatchExtractSentiment", input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}