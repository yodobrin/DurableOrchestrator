using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator;

[ActivitySource(nameof(WorkflowOrc))]
public class WorkflowOrc : BaseWorkflow
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(WorkflowOrc));
    private const string OrchestrationName = "WorkflowOrc";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";
    public WorkflowOrc() : base(nameof(WorkflowOrc)) { }

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        using var span = _tracer.StartActiveSpan(OrchestrationName);

        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new List<string>();

        var workFlowInput = context.GetInput<WorkFlowInput>();
        ValidationResult validationResult = ValidateWorkFlowInputs(workFlowInput!);
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(validationResult.Errors);
            log.LogError("WorkFlowInput is invalid.");
            return orchestrationResults; // Exit the orchestration due to validation errors
        }
        else
        {
            orchestrationResults.Add("WorkFlowInput is valid.");
            log.LogInformation("WorkFlowInput is valid.");
        }

        // Step 1: Retrieve the secret value
        try
        {
            var secretName = workFlowInput!.Name;
            var secretValue = await context.CallActivityAsync<string>(nameof(KeyVaultActivities.GetSecretFromKeyVault), secretName);
            orchestrationResults.Add($"Successfully retrieved secret: {secretName}");
            if (string.IsNullOrEmpty(secretValue))
            {
                log.LogError("Secret value is null or empty.");
                orchestrationResults.Add($"Error: Secret value is null or empty.");
                return orchestrationResults;
            }
            // Update BlobStorageInfo with the secret value
            workFlowInput!.TargetBlobStorageInfo!.Content = secretValue;

            // Step 2: Write the secret value to blob storage
            await context.CallActivityAsync(nameof(BlobStorageActivities.WriteStringToBlob), workFlowInput.TargetBlobStorageInfo);
            orchestrationResults.Add($"Successfully stored secret '{secretName}' in blob storage.");
        }
        catch (System.Exception ex)
        {
            log.LogError("Error during orchestration: {Message}", ex.Message);
            orchestrationResults.Add($"Error: {ex.Message}");
        }

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
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestrationName, input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
