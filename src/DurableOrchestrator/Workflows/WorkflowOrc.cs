using DurableOrchestrator.KeyVault;
using DurableOrchestrator.Models;
using DurableOrchestrator.Observability;
using DurableOrchestrator.Storage;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(WorkflowOrc))]
public class WorkflowOrc(ObservabilitySettings observabilitySettings)
    : BaseWorkflow(nameof(WorkflowOrc), observabilitySettings)
{
    private const string OrchestrationName = "WorkflowOrc";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var workFlowInput = context.GetInput<WorkFlowInput>() ??
                            throw new ArgumentNullException(nameof(context), "WorkFlowInput is null.");

        using var span = StartActiveSpan(OrchestrationName, workFlowInput);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new List<string>();

        var validationResult = ValidateWorkFlowInputs(workFlowInput);
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(validationResult.ValidationMessages);
            log.LogError($"WorkflowOrc::WorkFlowInput is invalid. {validationResult.GetValidationMessages()}");
            return orchestrationResults; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add("WorkFlowInput is valid.");
        log.LogInformation("WorkflowOrc::WorkFlowInput is valid.");

        // Step 1: Retrieve the secret value
        try
        {
            var secretName = workFlowInput.Name;

            var secretInput = new KeyVaultRequest { SecretName = secretName };
            InjectTracingContext(secretInput, span.Context);

            var secretValue = await context.CallActivityAsync<string>(
                nameof(KeyVaultActivities.GetSecretFromKeyVault),
                secretInput);
            orchestrationResults.Add($"Successfully retrieved secret: {secretName}");

            if (string.IsNullOrEmpty(secretValue))
            {
                log.LogError("Secret value is null or empty.");
                orchestrationResults.Add($"Error: Secret value is null or empty.");
                return orchestrationResults;
            }

            // Update BlobStorageInfo with the secret value
            workFlowInput.TargetBlobStorageInfo!.Content = secretValue;
            InjectTracingContext(workFlowInput.TargetBlobStorageInfo!, span.Context);

            // Step 2: Write the secret value to blob storage
            await context.CallActivityAsync(
                nameof(BlobStorageActivities.WriteStringToBlob),
                workFlowInput.TargetBlobStorageInfo);
            orchestrationResults.Add($"Successfully stored secret '{secretName}' in blob storage.");
        }
        catch (Exception ex)
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

        // Function input comes from the request content.
        var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(OrchestrationName, input);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
