using DurableOrchestrator.AzureStorage;


namespace DurableOrchestrator;
public abstract class WorkflowRequestBase : IWorkflowRequest
{
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    public abstract ValidationResult Validate();

    protected ValidationResult ValidateBlobStorageInfo(BlobStorageRequest? info, string propertyName)
    {
        var result = new ValidationResult();
        if (info == null)
        {
            result.AddErrorMessage($"{propertyName} blob storage info is missing.");
        }
        else
        {
            if (string.IsNullOrEmpty(info.BlobName))
            {
                result.AddMessage($"{propertyName} blob name is missing.");
            }
            if (string.IsNullOrEmpty(info.ContainerName))
            {
                result.AddErrorMessage($"{propertyName} container name is missing.");
            }
            if (string.IsNullOrEmpty(info.StorageAccountName))
            {
                result.AddErrorMessage($"{propertyName} storage account name is missing.");
            }
        }
        return result;
    }
}
