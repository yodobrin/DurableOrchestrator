using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Models;

public class BlobStorageInfo : IObservabilityContext
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

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();
}
