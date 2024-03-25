using System.Text.Json.Serialization;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a model that represents information about a blob in Azure Storage.
/// </summary>
public class BlobStorageInfo : IObservabilityContext
{
    /// <summary>
    /// Gets or sets the name of the storage account.
    /// </summary>
    [JsonPropertyName("storageAccountName")]
    public string StorageAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the blob.
    /// </summary>
    [JsonPropertyName("blobName")]
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the blob container.
    /// </summary>
    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI of the blob.
    /// </summary>
    [JsonPropertyName("blobUri")]
    public string BlobUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the blob.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buffer of the blob.
    /// </summary>
    [JsonPropertyName("buffer")]
    public byte[] Buffer { get; set; } = Array.Empty<byte>();

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();
}
