using DurableOrchestrator.AzureOpenAI;
using DurableOrchestrator.AzureStorage;
// using DurableOrchestrator.AzureTextAnalytics;
// using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Workflows;

[ActivitySource(nameof(TextAnalyticsWorkflow))]
public class EmbedTextWorkFlow()
    : BaseWorkflow(nameof(EmbedTextWorkFlow))
{
    private const string OrchestrationName = "EmbedTextWorkFlow";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    
    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<EmbeddingWorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context), $"{nameof(EmbeddingWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(EmbeddingWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(EmbeddingWorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3:
        // calling OpenAIActivity to embed the text, first need to create the request

        OpenAIRequest openAIRequest = new OpenAIRequest
        {
            EmbeddedDeployment = input.EmbeddedDeployment,
            OpenAIOperation = OpenAIOperation.Embedding,
            Text2Embed = input.Text2Embed
        };

        float[] embeddings = await CallActivityAsync<float[]>(context, nameof(OpenAIActivities.EmbeddText), openAIRequest, span.Context);

        // lets write the embedding to a file as well
        var options = new JsonSerializerOptions { WriteIndented = true };
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(embeddings, options);

        var embeddingBlobStorageInfo = input.TargetBlobStorageInfo!;
        // override the blob name and the content
        embeddingBlobStorageInfo.BlobName = $"{input.TargetBlobStorageInfo!.BlobName}_embeddings.json";
        embeddingBlobStorageInfo.Buffer = jsonBytes;
        // embeddingBlobStorageInfo.InjectTracingContext(span.Context);
        await context.CallActivityAsync<string>(nameof(BlobStorageActivities.WriteBufferToBlob), embeddingBlobStorageInfo);

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
            ExtractInput<EmbeddingWorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }
    public class EmbeddingWorkflowRequest : WorkflowRequestBase  
    {  
        [JsonPropertyName("embeddedDeployment")]  
        public string EmbeddedDeployment { get; set; } = string.Empty;  
    
        [JsonPropertyName("targetBlobStorageInfo")]  
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }  
    
        [JsonPropertyName("text2embed")]  
        public string Text2Embed { get; set; } = string.Empty;  
    
        public override ValidationResult Validate()  
        {  
            var result = new ValidationResult();  
            if (string.IsNullOrEmpty(EmbeddedDeployment))  
            {  
                result.AddErrorMessage("Embedded deployment is missing.");  
            }  
            if (string.IsNullOrWhiteSpace(Text2Embed))  
            {  
                result.AddErrorMessage("Text to embed is missing.");  
            }  
            result.Merge(ValidateBlobStorageInfo(TargetBlobStorageInfo, "Target"));  
            return result;  
        }  
    }  

}
