using Microsoft.Azure.Functions.Worker.Extensions.EventHubs;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;
// using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DurableOrchestrator.Examples.Scale;

[ActivitySource]
public class ParquetOrcWorkflow(StorageClientFactory storageClientFactory) : BaseWorkflow(OrchestrationName)
{
    
    private const string OrchestrationName = nameof(ParquetOrcWorkflow);
    private const string OrchestrationTriggerName = $"{OrchestrationName}_{nameof(EventHubStart)}";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<Json2ParquetOrchestrationRequest>() ??
                    throw new ArgumentNullException(nameof(context),
                        $"{nameof(Json2ParquetOrchestrationRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);
        // return orchestrationResults.Results;
        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(Json2ParquetOrchestrationRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        orchestrationResults.Add(nameof(Json2ParquetOrchestrationRequest.Validate), $"{nameof(input)} is valid.");
        // step 3, pages per json2parquet workflow
        try{

            var containerClient = storageClientFactory
                .GetBlobServiceClient(input.SourceBlobStorageInfo!.StorageAccountName)
                .GetBlobContainerClient(input.SourceBlobStorageInfo.ContainerName);
            
            List<Task<bool>> workflows = new List<Task<bool>>();
            int pageSize = 100;
            string? continuationToken = null;
            
            do
            {
                var page = await CallActivityAsync<BlobPagination>(
                    context,
                    nameof(BlobStorageActivities.GetBlobsPage),
                    new BlobPagination
                    {
                        ContainerName = input.SourceBlobStorageInfo.ContainerName,
                        StorageAccountName = input.SourceBlobStorageInfo.StorageAccountName,
                        BlobName = string.Empty, // so it wont fail validation
                        PageSize = pageSize,
                        ContinuationToken = continuationToken
                    },
                    span.Context
                );
                Task<bool> json2parquet = CallActivityAsync<bool>(
                    context,
                    nameof(BlobStorageActivities.Json2Parquet),
                    new CompoundStorageRequest
                    {
                        SourceStorageRequest = input.SourceBlobStorageInfo!,
                        DestinationStorageRequest = input.TargetBlobStorageInfo!,
                        BlobNames = page.BlobNames
                    },
                    span.Context
                );
                workflows.Add(json2parquet);
                continuationToken = page.ContinuationToken;
            }while (continuationToken != null);
            // Await all the tasks to complete - this is the fan-in part
            bool[] allResults = await Task.WhenAll(workflows);
            // Check if all the tasks completed successfully
            if (allResults.All(result => result))
            {
                orchestrationResults.Add(nameof(Json2ParquetOrchestrationRequest.Validate), $"All pages processed successfully");
            }
            else
            {
                orchestrationResults.Add(nameof(Json2ParquetOrchestrationRequest.Validate), $"Some pages failed to process");
            }

        }catch (Exception e){
            orchestrationResults.Add(nameof(Json2ParquetOrchestrationRequest.Validate), $"Error in running pages per json2parquet workflow: {e.Message}");
            return orchestrationResults.Results;
        }
        
        orchestrationResults.Add(nameof(ParquetOrcWorkflow), $"Json to parquet orchestration completed ");
        
        return orchestrationResults.Results;
    }

    [Function(OrchestrationTriggerName)]
    public async Task EventHubStart(
        [EventHubTrigger("json2parquet", Connection = "JSON2PARQUET_EVENTHUB", IsBatched = false)]
        Json2ParquetOrchestrationRequest orcRequest,
        [DurableClient] DurableTaskClient starter,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(OrchestrationTriggerName);
        var log = executionContext.GetLogger(OrchestrationTriggerName);
        if (orcRequest == null)
        {
            throw new ArgumentNullException(nameof(orcRequest), $"{nameof(Json2ParquetOrchestrationRequest)} is null.");
        }
        // log.LogInformation($"Received request: {orcRequest}");

        var instanceId = await StartWorkflowAsync(
            starter,
            orcRequest,
            span.Context);

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        
    }

    public class Json2ParquetOrchestrationRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("fanOut")]
        public int FanOut { get; set; } = 1;
        [JsonPropertyName("folder")]
        public string ? Folder { get; set; } = string.Empty;
        [JsonPropertyName("sourceBlobStorageInfo")]
        public BlobStorageRequest? SourceBlobStorageInfo { get; set; }

        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

        [JsonPropertyName("containerSize")]
        public ContainerSize containerSize { get; set; } = ContainerSize.Small;

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            result.Merge(SourceBlobStorageInfo?.Validate(checkContent: false), "Source blob storage info is missing.");
            result.Merge(TargetBlobStorageInfo?.Validate(checkContent: false), "Target blob storage info is missing.");

            return result;
        }

        public enum ContainerSize
        {
            Small, // less than 100 blobs
            Medium, // 100 to 1000 blobs
            Large // more than 1000 blobs
        }
    }
}
    