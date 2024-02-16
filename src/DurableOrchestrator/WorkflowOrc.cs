namespace DurableOrchestrator;

public static class WorkflowOrc
{
    [Function("WorkflowOrc")]
    public static async Task<List<string>> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var log = context.CreateReplaySafeLogger("WorkflowOrc");

        var orchestrationResults = new List<string>();

        var workFlowInput = context.GetInput<WorkFlowInput>();
        if (workFlowInput == null)
        {
            log.LogError("WorkFlowInput is null.");
            orchestrationResults.Add("Workflow input was not provided.");
            return orchestrationResults; // Exit the orchestration due to missing WorkFlowInput
        }

        if (string.IsNullOrEmpty(workFlowInput.Name) || workFlowInput.BlobStorageInfo == null ||
            string.IsNullOrEmpty(workFlowInput.BlobStorageInfo.BlobName) ||
            string.IsNullOrEmpty(workFlowInput.BlobStorageInfo.ContainerName))
        {
            log.LogError("Missing required details in WorkFlowInput or BlobStorageInfo.");
            orchestrationResults.Add("Missing required details in WorkFlowInput or BlobStorageInfo.");
            return orchestrationResults; // Exit the orchestration due to missing required details
        }

        // Step 1: Retrieve the secret value
        try
        {
            var secretName = workFlowInput.Name;
            var secretValue = await context.CallActivityAsync<string>("GetSecretFromKeyVault", secretName)
                .ConfigureAwait(false);
            orchestrationResults.Add($"Successfully retrieved secret: {secretName}");

            // Update BlobStorageInfo with the secret value
            workFlowInput.BlobStorageInfo.Content = secretValue;

            // Step 2: Write the secret value to blob storage
            await context.CallActivityAsync("WriteStringToBlob", workFlowInput.BlobStorageInfo).ConfigureAwait(false);
            orchestrationResults.Add($"Successfully stored secret '{secretName}' in blob storage.");
        }
        catch (System.Exception ex)
        {
            log.LogError("Error during orchestration: {Message}", ex.Message);
            orchestrationResults.Add($"Error: {ex.Message}");
        }

        return orchestrationResults;
    }

    [Function("WorkflowOrc_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient starter,
        FunctionContext executionContext)
    {
        var log = executionContext.GetLogger("WorkflowOrc_HttpStart");

        var requestBody = await req.ReadAsStringAsync().ConfigureAwait(false);

        // Check for an empty request body as a more direct approach
        if (string.IsNullOrEmpty(requestBody))
        {
            throw new ArgumentException("The request body must not be null or empty.", nameof(req));
        }

        var input = JsonSerializer.Deserialize<WorkFlowInput>(requestBody) ??
                    throw new ArgumentException("The request body is not a valid WorkFlowInput.", nameof(req));

        // Function input comes from the request content.
        var instanceId =
            await starter.ScheduleNewOrchestrationInstanceAsync("WorkflowOrc", input).ConfigureAwait(false);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId).ConfigureAwait(false);
    }
}
