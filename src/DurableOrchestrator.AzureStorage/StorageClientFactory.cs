using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a factory for creating Azure Storage service client instances.
/// </summary>
/// <param name="azureCredential">The Azure credential to use for authentication with Azure Storage clients.</param>
public class StorageClientFactory(DefaultAzureCredential azureCredential)
{
    /// <summary>
    /// Retrieves a <see cref="BlobServiceClient"/> for the specified storage account name.
    /// </summary>
    /// <remarks>
    /// If the specified storage account name is a development storage account (i.e., devstoreaccount1 or UseDevelopmentStorage), the client will be created using the development storage connection string.
    /// </remarks>
    /// <param name="storageAccountName">The name of the storage account to create a client for.</param>
    /// <returns>A <see cref="BlobServiceClient"/> for the specified storage account name.</returns>
    public BlobServiceClient GetBlobServiceClient(string storageAccountName)
    {
        return IsDevelopmentStorageAccount(storageAccountName)
            ? new BlobServiceClient("UseDevelopmentStorage=true")
            : new BlobServiceClient(
                new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                azureCredential);
    }

    /// <summary>
    /// Retrieves a <see cref="TableServiceClient"/> for the specified storage account name.
    /// </summary>
    /// <remarks>
    /// If the specified storage account name is a development storage account (i.e., devstoreaccount1 or UseDevelopmentStorage), the client will be created using the development storage connection string.
    /// </remarks>
    /// <param name="storageAccountName">The name of the storage account to create a client for.</param>
    /// <returns>A <see cref="TableServiceClient"/> for the specified storage account name.</returns>
    public TableServiceClient GetTableServiceClient(string storageAccountName)
    {
        return IsDevelopmentStorageAccount(storageAccountName)
            ? new TableServiceClient("UseDevelopmentStorage=true")
            : new TableServiceClient(
                new Uri($"https://{storageAccountName}.table.core.windows.net"),
                azureCredential);
    }

    private static bool IsDevelopmentStorageAccount(string storageAccountName)
    {
        return !string.IsNullOrWhiteSpace(storageAccountName) &&
               (storageAccountName.Equals("devstoreaccount1", StringComparison.OrdinalIgnoreCase) ||
                storageAccountName.StartsWith("UseDevelopmentStorage", StringComparison.OrdinalIgnoreCase));
    }
}
