using DurableOrchestrator.AzureKeyVault;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Workflows;

[ActivitySource]
public class KeyVaultSecretToBlobWorkflow() : BaseWorkflow(OrchestrationName)
{
    private const string OrchestrationName = nameof(KeyVaultSecretToBlobWorkflow);
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<KeyVaultSecretToBlobWorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context), $"{nameof(KeyVaultSecretToBlobWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(KeyVaultSecretToBlobWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(KeyVaultSecretToBlobWorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3: retrieve the secret from Key Vault
        var secretName = input.Name;

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
        input.TargetBlobStorageInfo!.Content = secretValue;

        try
        {
            await CallActivityAsync(
                context,
                nameof(BlobStorageActivities.WriteStringToBlob),
                input.TargetBlobStorageInfo!,
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
            new SplitPdfWorkflow.SplitPdfWorkflowRequest
            {
                SourceBlobStorageInfo = input.SourceBlobStorageInfo,
                TargetBlobStorageInfo = input.TargetBlobStorageInfo,
            },
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
            ExtractInput<KeyVaultSecretToBlobWorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }

    internal class KeyVaultSecretToBlobWorkflowRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sourceBlobStorageInfo")]
        public BlobStorageRequest? SourceBlobStorageInfo { get; set; }

        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(Name))
            {
                result.AddMessage("Secret name is missing.");
            }

            result.Merge(SourceBlobStorageInfo?.Validate(checkContent: false), "Source blob storage info is missing.");
            result.Merge(TargetBlobStorageInfo?.Validate(checkContent: false), "Target blob storage info is missing.");

            return result;
        }
    }
}
