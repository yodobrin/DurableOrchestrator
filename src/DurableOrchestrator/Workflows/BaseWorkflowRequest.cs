using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.Workflows;

public abstract class BaseWorkflowRequest : IWorkflowRequest
{
    [JsonPropertyName("targetBlobStorageInfo")]
    public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (TargetBlobStorageInfo == null)
        {
            result.AddErrorMessage("Target blob storage info is missing.");
        }
        else
        {
            if (string.IsNullOrEmpty(TargetBlobStorageInfo.BlobName))
            {
                // could be missing - not breaking the validity of the request
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

        return result;
    }
}
