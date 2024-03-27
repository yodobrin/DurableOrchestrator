using DurableOrchestrator.AzureOpenAI;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Workflows;

[ActivitySource]
public class EmbedTextWorkFlow() : BaseWorkflow(OrchestrationName)
{
    private const string OrchestrationName = nameof(EmbedTextWorkFlow);
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<EmbedTextWorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context), $"{nameof(EmbedTextWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(EmbedTextWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(EmbedTextWorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3:
        // calling OpenAIActivity to embed the text, first need to create the request
        var embeddings = await CallActivityAsync<float[]?>(
            context,
            nameof(OpenAIActivities.EmbedText),
            input.EmbeddingInfo!,
            span.Context);

        // then, write the embedding as JSON to a file
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(embeddings, options);

        var embeddingBlobStorageInfo = input.TargetBlobStorageInfo!;
        embeddingBlobStorageInfo.BlobName = $"{input.TargetBlobStorageInfo!.BlobName}_embeddings.json";
        embeddingBlobStorageInfo.Buffer = jsonBytes;

        await CallActivityAsync<string>(
            context,
            nameof(BlobStorageActivities.WriteBufferToBlob),
            embeddingBlobStorageInfo,
            span.Context);

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
            ExtractInput<EmbedTextWorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }

    internal class EmbedTextWorkflowRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

        [JsonPropertyName("embeddingInfo")]
        public OpenAIEmbeddingRequest? EmbeddingInfo { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            result.Merge(TargetBlobStorageInfo?.Validate(checkContent: false), "Target blob storage info is missing.");
            result.Merge(EmbeddingInfo?.Validate(), "Embedding info is missing.");

            return result;
        }
    }
}
