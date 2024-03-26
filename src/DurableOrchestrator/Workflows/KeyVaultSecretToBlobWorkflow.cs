using DurableOrchestrator.AzureKeyVault;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(KeyVaultSecretToBlobWorkflow))]
public class KeyVaultSecretToBlobWorkflow()
    : BaseWorkflow(nameof(KeyVaultSecretToBlobWorkflow))
{
    private const string OrchestrationName = "KeyVaultSecretToBlobWorkflow";
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

        // step 3: retrieve the secret from Key Vault
        var secretName = workFlowInput.Name;

        var secretValue = await CallActivityAsync<string>(
            context,
            nameof(KeyVaultActivities.GetSecretFromKeyVault),
            new KeyVaultSecretRequest { SecretName = secretName },
            span.Context);

        if (string.IsNullOrEmpty(secretValue))
        {
            orchestrationResults.Add(
                nameof(KeyVaultActivities.GetSecretFromKeyVault),
                $"{secretName} value is empty or null.",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to missing secret value
        }

        orchestrationResults.Add(
            nameof(KeyVaultActivities.GetSecretFromKeyVault),
            $"{secretName} value successfully retrieved.");

        // step 4: write the secret value to blob storage
        workFlowInput.TargetBlobStorageInfo!.Content = secretValue;

        try
        {
            await CallActivityAsync(
                context,
                nameof(BlobStorageActivities.WriteStringToBlob),
                workFlowInput.TargetBlobStorageInfo!,
                span.Context);

            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteStringToBlob),
                $"{secretName} value saved to blob storage.");
        }
        catch (Exception ex)
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.WriteStringToBlob),
                $"{secretName} value failed to save to blob storage. {ex.Message}",
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to an error during the write operation
        }

        // step 5: run the split PDF workflow on the saved secret value
        var splitPdfResult = await CallWorkflowAsync<List<string>>(
            context,
            nameof(SplitPdfWorkflow),
            workFlowInput,
            span.Context);

        orchestrationResults.AddRange(nameof(SplitPdfWorkflow), $"{nameof(splitPdfResult)} completed.", splitPdfResult);

        return orchestrationResults.Results;
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

        var instanceId = await StartWorkflowAsync(
            starter,
            OrchestrationName,
            ExtractInput<WorkFlowInput>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
