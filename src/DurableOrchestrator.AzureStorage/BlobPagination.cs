using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureStorage;

public class BlobPagination : BaseWorkflowRequest
{
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 100;

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("blobNames")]
    public List<string> BlobNames { get; set; } = new List<string>();

    [JsonPropertyName("storageAccountName")]
    public string StorageAccountName { get; set; } = string.Empty;
    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; } = string.Empty;
    public override ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (PageSize <= 0)
        {
            result.AddErrorMessage($"{nameof(PageSize)} must be greater than 0.");
        }
        if (string.IsNullOrEmpty(StorageAccountName))
        {
            result.AddErrorMessage($"{nameof(StorageAccountName)} is missing.");
        }
        if (string.IsNullOrEmpty(ContainerName))
        {
            result.AddErrorMessage($"{nameof(ContainerName)} is missing.");
        }

        return result;
    }

}