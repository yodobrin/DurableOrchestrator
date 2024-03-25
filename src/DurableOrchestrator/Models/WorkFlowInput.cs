using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Models;

public class WorkFlowInput : IWorkflowInput
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")] public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("sourceBlobStorageInfo")]
    public BlobStorageInfo? SourceBlobStorageInfo { get; set; }

    [JsonPropertyName("targetBlobStorageInfo")]
    public BlobStorageInfo? TargetBlobStorageInfo { get; set; }

    [JsonPropertyName("textAnalyticsRequests")]
    public List<TextAnalyticsRequest>? TextAnalyticsRequests { get; set; } = new();

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        // Source and Target Blobs while marked as optional are required for the workflow to proceed
        if (string.IsNullOrEmpty(Name))
        {
            result.AddMessage("Workflow name is missing.");
        }

        if (SourceBlobStorageInfo == null)
        {
            result.AddErrorMessage("Source blob storage info is missing.");
        }
        else
        {
            if (string.IsNullOrEmpty(SourceBlobStorageInfo.BlobName))
            {
                result.AddMessage("Source blob name is missing.");
                // could be missing - not breaking the validity of the request
            }

            if (string.IsNullOrEmpty(SourceBlobStorageInfo.ContainerName))
            {
                result.AddErrorMessage("Source container name is missing.");
            }

            if (string.IsNullOrEmpty(SourceBlobStorageInfo.StorageAccountName))
            {
                result.AddErrorMessage("Source storage account name is missing.");
            }
        }

        if (TargetBlobStorageInfo == null)
        {
            result.AddErrorMessage("Target blob storage info is missing.");
        }
        else
        {
            if (string.IsNullOrEmpty(TargetBlobStorageInfo.BlobName))
            {
                result.AddMessage("Target blob name is missing.");
            }

            if (string.IsNullOrEmpty(TargetBlobStorageInfo.ContainerName))
            {
                result.AddErrorMessage("Target container name is missing.");
            }

            if (string.IsNullOrEmpty(TargetBlobStorageInfo.StorageAccountName))
            {
                result.AddErrorMessage("Target storage account name is missing.");
            }
        }

        if (TextAnalyticsRequests == null || TextAnalyticsRequests.Count == 0)
        {
            result.AddMessage("TextAnalyticsRequests is missing or empty.");
        }
        else
        {
            // only if the individual list is empty -> request is not valid
            foreach (var request in TextAnalyticsRequests)
            {
                if (string.IsNullOrEmpty(request.TextsToAnalyze))
                {
                    result.AddErrorMessage("TextsToAnalyze is missing or empty.");
                }

                if (request.OperationTypes == null || request.OperationTypes.Count == 0)
                {
                    result.AddErrorMessage("OperationTypes is missing or empty - what should be done with the text?");
                }
            }
        }

        return result;
    }
}
