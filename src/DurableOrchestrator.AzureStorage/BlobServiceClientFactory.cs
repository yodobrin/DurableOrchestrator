using Azure.Identity;
using Azure.Storage.Blobs;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a wrapper for a source and target <see cref="BlobServiceClient"/>.
/// </summary>
/// <param name="azureCredential">The Azure credential to use for authentication with Azure Blob Storage clients.</param>
public class BlobServiceClientFactory(DefaultAzureCredential azureCredential)
{
    public BlobServiceClient GetBlobServiceClient(string storageAccountName)
    {
        return IsDevelopmentStorageAccount(storageAccountName)
            ? new BlobServiceClient("UseDevelopmentStorage=true")
            : new BlobServiceClient(
                new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                azureCredential);
    }

    private static bool IsDevelopmentStorageAccount(string storageAccountName)
    {
        return !string.IsNullOrWhiteSpace(storageAccountName) &&
               (storageAccountName.Equals("devstoreaccount1", StringComparison.OrdinalIgnoreCase) ||
                storageAccountName.StartsWith("UseDevelopmentStorage", StringComparison.OrdinalIgnoreCase));
    }
}
