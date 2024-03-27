using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a model that represents information about a blob in Azure Storage.
/// </summary>
public class BlobStorageRequest : IWorkflowRequest
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

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        return Validate(true);
    }

    /// <summary>
    /// Validates the input with an option to check the content.
    /// </summary>
    /// <param name="checkContent">A flag indicating whether to check the content.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the input is valid.</returns>
    public ValidationResult Validate(bool checkContent)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(StorageAccountName))
        {
            result.AddErrorMessage($"{nameof(StorageAccountName)} is missing.");
        }

        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            result.AddErrorMessage($"{nameof(ContainerName)} is missing.");
        }

        if (string.IsNullOrWhiteSpace(BlobName))
        {
            // could be missing - not breaking the validity of the request
            result.AddMessage($"{nameof(BlobName)} is missing.");
        }

        if (checkContent && string.IsNullOrWhiteSpace(Content))
        {
            result.AddErrorMessage($"{nameof(Content)} is missing.");
        }

        return result;
    }
}
