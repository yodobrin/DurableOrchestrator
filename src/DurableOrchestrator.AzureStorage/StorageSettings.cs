using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines the settings for configuring Azure Storage.
/// </summary>
/// <param name="blobSourceStorageAccountName">The name of the source Azure Storage account.</param>
/// <param name="blobTargetStorageAccountName">The name of the target Azure Storage account.</param>
public class StorageSettings(string blobSourceStorageAccountName, string blobTargetStorageAccountName)
{
    /// <summary>
    /// The configuration key for the source Azure Storage account name.
    /// </summary>
    public const string BlobSourceStorageAccountNameConfigKey = "BlobSourceStorageAccountName";

    /// <summary>
    /// The configuration key for the target Azure Storage account name.
    /// </summary>
    public const string BlobTargetStorageAccountNameConfigKey = "BlobTargetStorageAccountName";

    /// <summary>
    /// Gets the name of the source Azure Storage account.
    /// </summary>
    public string BlobSourceStorageAccountName { get; init; } = blobSourceStorageAccountName;

    /// <summary>
    /// Gets the name of the target Azure Storage account.
    /// </summary>
    public string BlobTargetStorageAccountName { get; init; } = blobTargetStorageAccountName;

    /// <summary>
    /// Creates a new instance of the <see cref="StorageSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="StorageSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the BlobSourceStorageAccountName or BlobTargetStorageAccountName is not configured.</exception>
    public static StorageSettings FromConfiguration(IConfiguration configuration)
    {
        var blobSourceStorageAccountName = configuration.GetValue<string>(BlobSourceStorageAccountNameConfigKey) ??
                                           throw new InvalidOperationException(
                                               "BlobSourceStorageAccountName is not configured.");
        var blobTargetStorageAccountName = configuration.GetValue<string>(BlobTargetStorageAccountNameConfigKey) ??
                                           throw new InvalidOperationException(
                                               "BlobTargetStorageAccountName is not configured.");

        return new StorageSettings(blobSourceStorageAccountName, blobTargetStorageAccountName);
    }
}
