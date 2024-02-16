namespace DurableOrchestrator;

public class WorkFlowInput
{

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("blobStorageInfo")]
    public BlobStorageInfo? BlobStorageInfo { get; set; }

}

public class BlobStorageInfo
{
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
