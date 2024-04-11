
// using Microsoft.Azure.Functions.Worker.Extensions.EventHubs;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Examples.Scale;

[ActivitySource]
public class Json2ParquetWorkflow() : BaseWorkflow(OrchestrationName)
{
    
    private const string OrchestrationName = nameof(Json2ParquetWorkflow);
    // private const string OrchestrationTriggerName = $"{OrchestrationName}_{nameof(EventHubStart)}";
    private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<Json2ParquetWorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context),
                        $"{nameof(Json2ParquetWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);
        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(Json2ParquetWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        orchestrationResults.Add(nameof(Json2ParquetWorkflowRequest.Validate), $"{nameof(input)} is valid.");
        // step 3: calling the activity function to read a file from the source blob storage
        // and calling activity to convert json to parquet
        CompoundStorageRequest request = new CompoundStorageRequest
        {
            SourceStorageRequest = input.SourceBlobStorageInfo!,
            DestinationStorageRequest = input.TargetBlobStorageInfo!,
            BlobNames = new List<string>(input.BlobNames)
        };        
        var parquetContent = await CallActivityAsync<bool>(
            context,
            nameof(BlobStorageActivities.Json2Parquet),
            request,
            span.Context);

        
        orchestrationResults.Add(nameof(BlobStorageActivities.Json2Parquet), $"Json to parquet conversion completed with success: {parquetContent}");
        
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
            ExtractInput<Json2ParquetWorkflowRequest>(requestBody),
            span.Context);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await starter.CreateCheckStatusResponseAsync(req, instanceId);
    }

    public class Json2ParquetWorkflowRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("fanOut")]
        public int FanOut { get; set; } = 1;
        [JsonPropertyName("folder")]
        public string ? Folder { get; set; } = string.Empty;
        [JsonPropertyName("sourceBlobStorageInfo")]
        public BlobStorageRequest? SourceBlobStorageInfo { get; set; }

        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }
        [JsonPropertyName("blobNames")]
        public List<string> BlobNames { get; set; } = new List<string>();
        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            result.Merge(SourceBlobStorageInfo?.Validate(checkContent: false), "Source blob storage info is missing.");
            result.Merge(TargetBlobStorageInfo?.Validate(checkContent: false), "Target blob storage info is missing.");

            if (BlobNames == null || BlobNames.Count == 0)
            {
                result.AddErrorMessage($"{nameof(BlobNames)} is missing or empty.");
            }

            return result;
        }
    }
}