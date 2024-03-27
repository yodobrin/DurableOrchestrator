using DurableOrchestrator.AzureOpenAI;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
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
        var input = context.GetInput<WorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context), $"{nameof(WorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(WorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results; // Exit the orchestration due to validation errors
        }

        orchestrationResults.Add(nameof(WorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3:
        // calling OpenAIActivity to embed the text, first need to create the request

        var openAIRequest = new OpenAIRequest
        {
            EmbeddedDeployment = input.EmbeddedDeployment,
            OpenAIOperation = OpenAIOperation.Embedding,
            Text2Embed = input.Text2Embed
        };

        var embeddings = await CallActivityAsync<float[]>(
            context,
            nameof(OpenAIActivities.EmbeddText),
            openAIRequest,
            span.Context);

        // lets write the embedding to a file as well
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(embeddings, options);

        var embeddingBlobStorageInfo = input.TargetBlobStorageInfo!;
        // override the blob name and the content
        embeddingBlobStorageInfo.BlobName = $"{input.TargetBlobStorageInfo!.BlobName}_embeddings.json";
        embeddingBlobStorageInfo.Buffer = jsonBytes;
        // embeddingBlobStorageInfo.InjectTracingContext(span.Context);
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
            ExtractInput<WorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }

    public class WorkflowRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("embeddedDeployment")]
        public string EmbeddedDeployment { get; set; } = string.Empty;

        [JsonPropertyName("text2embed")] public string Text2Embed { get; set; } = string.Empty;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            if (string.IsNullOrEmpty(EmbeddedDeployment))
            {
                result.AddErrorMessage("Embedded deployment is missing.");
            }

            if (string.IsNullOrWhiteSpace(Text2Embed))
            {
                result.AddErrorMessage("Text to embed is missing.");
            }

            return result;
        }
    }
}
