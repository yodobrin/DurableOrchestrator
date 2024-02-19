namespace DurableOrchestrator;

public class WorkFlowInput
{

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("sourceBlobStorageInfo")]
    public BlobStorageInfo? SourceBlobStorageInfo { get; set; }
    
    [JsonPropertyName("targetBlobStorageInfo")]
    public BlobStorageInfo? TargetBlobStorageInfo { get; set; }
}

public class BlobStorageInfo
{
    [JsonPropertyName("storageAccountName")]
    public string StorageAccountName { get; set; } = string.Empty;
    [JsonPropertyName("blobName")]
    public string BlobName { get; set; } = string.Empty;

    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; } = string.Empty;

    [JsonPropertyName("blobUri")]
    public string BlobUri { get; set; } = string.Empty;
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    [JsonPropertyName("buffer")]
    public byte[] Buffer { get; set; } = Array.Empty<byte>();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}

// public class ExtractAndValidateResult
// {
//     public WorkFlowInput WorkFlowInput { get; set; }
//     public ValidationResult ValidationResult { get; set; }

//     public ExtractAndValidateResult(WorkFlowInput workFlowInput, ValidationResult validationResult)
//     {
//         WorkFlowInput = workFlowInput;
//         ValidationResult = validationResult;
//     }
// }
