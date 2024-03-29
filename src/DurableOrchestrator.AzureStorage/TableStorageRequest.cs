using System.Text.Json.Serialization;
using Azure.Data.Tables;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Serialization;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a model that represents information about a table in Azure Storage.
/// </summary>
public class TableStorageRequest : StorageAccountRequest
{
    /// <summary>
    /// Gets or sets the name of the table.
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity to store in the table.
    /// </summary>
    [JsonPropertyName("entity")]
    [JsonConverter(typeof(ObjectToTypeConverter<ITableEntity>))]
    public ITableEntity? Entity { get; set; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (string.IsNullOrWhiteSpace(TableName))
        {
            result.AddErrorMessage($"{nameof(TableName)} is missing.");
        }

        if (Entity == null)
        {
            // initially, the entity may be null.
            result.AddMessage($"{nameof(Entity)} is missing.");
        }

        return result;
    }
}
