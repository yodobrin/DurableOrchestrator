using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a base model that represents information about a storage account in Azure Storage.
/// </summary>
public abstract class StorageAccountRequest : BaseWorkflowRequest
{
    /// <summary>
    /// Gets or sets the name of the storage account.
    /// </summary>
    [JsonPropertyName("storageAccountName")]
    public string StorageAccountName { get; set; } = string.Empty;

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(StorageAccountName))
        {
            result.AddErrorMessage($"{nameof(StorageAccountName)} is missing.");
        }

        return result;
    }
}
