using System.Collections.Generic;
using System.Threading.Tasks;


namespace event_processing
{
    public static class WorkflowOrc
    {
        [FunctionName("WorkflowOrc")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
{
        List<string> orchestrationResults = new List<string>();

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
            string secretName = workFlowInput.Name;
            string secretValue = await context.CallActivityAsync<string>("GetSecretFromKeyVault", secretName);
            orchestrationResults.Add($"Successfully retrieved secret: {secretName}");

            // Update BlobStorageInfo with the secret value
            workFlowInput.BlobStorageInfo.Content = secretValue;

            // Step 2: Write the secret value to blob storage
            await context.CallActivityAsync("WriteStringToBlob", workFlowInput.BlobStorageInfo);
            orchestrationResults.Add($"Successfully stored secret '{secretName}' in blob storage.");
        }
        catch (System.Exception ex)
        {
            log.LogError($"Error during orchestration: {ex.Message}");
            orchestrationResults.Add($"Error: {ex.Message}");
        }

        return orchestrationResults;
    }


        [FunctionName("WorkflowOrc_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string requestBody = await req.Content!.ReadAsStringAsync();

            // Check for an empty request body as a more direct approach
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new ArgumentException("The request body must not be null or empty.", nameof(requestBody));
            }

            WorkFlowInput? input = JsonSerializer.Deserialize<WorkFlowInput>(requestBody);
            if (input == null)
            {
                throw new ArgumentException("The request body is not a valid WorkFlowInput.", nameof(requestBody));
            }

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("WorkflowOrc", input);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}