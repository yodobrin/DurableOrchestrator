using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.Storage;

internal static class StorageExtensions
{

    internal static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });
        return services;
    }

    public static IServiceCollection AddBlobStorageClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Instantiate source and target BlobServiceClient using configuration
        string SourceStorageAccountName = configuration["BlobSourceStorageAccountName"] ?? throw new InvalidOperationException("BlobSourceStorageAccountName is not configured.");
        string TargetStorageAccountName = configuration["BlobTargetStorageAccountName"] ?? throw new InvalidOperationException("BlobTargetStorageAccountName is not configured.");
        var sourceClient = new BlobServiceClient(new Uri($"https://{SourceStorageAccountName}.blob.core.windows.net"), new DefaultAzureCredential());
        var targetClient = new BlobServiceClient(new Uri($"https://{TargetStorageAccountName}.blob.core.windows.net"), new DefaultAzureCredential());

        // Register BlobServiceClientsWrapper with the DI container
        services.AddSingleton(new BlobServiceClientsWrapper(sourceClient, targetClient));

        return services;
    }

    // private static BlobServiceClient GetBlobServiceClient(BlobStorageInfo input)
    // {
    //     if (string.IsNullOrWhiteSpace(input.BlobUri))
    //     {
    //         if (string.IsNullOrWhiteSpace(input.StorageAccountName) || string.IsNullOrWhiteSpace(input.ContainerName))
    //         {
    //             throw new ArgumentException("Container name or Storage Account name is not provided, and no BlobUri is specified.");
    //         }

    //         // Assuming DefaultAzureCredential will pick up the necessary authentication details from the environment
    //         return new BlobServiceClient(new Uri($"https://{input.StorageAccountName}.blob.core.windows.net"), new DefaultAzureCredential());
    //     }
    //     else
    //     {
    //         return new BlobServiceClient(new Uri(input.BlobUri), new DefaultAzureCredential());
    //     }
    // }
    // internal static (BlobStorageInfo sourceInfo, BlobStorageInfo targetInfo) CreateBlobStorageInfos(IConfiguration configuration)
    // {
    //     var sourceInfo = new BlobStorageInfo
    //     {
    //         StorageAccountName = configuration["BlobSourceStorageAccountName"] ?? throw new InvalidOperationException("BlobSourceStorageAccountName is not configured."),
    //         BlobName = configuration["BlobSourceBlobName"] ?? throw new InvalidOperationException("BlobSourceBlobName is not configured."),
    //         ContainerName = configuration["BlobSourceContainerName"] ?? throw new InvalidOperationException("BlobSourceContainerName is not configured."),
    //     };

    //     var targetInfo = new BlobStorageInfo
    //     {
    //         StorageAccountName = configuration["BlobTargetStorageAccountName"] ?? throw new InvalidOperationException("BlobTargetStorageAccountName is not configured."),
    //         BlobName = configuration["BlobTargetBlobName"] ?? throw new InvalidOperationException("BlobTargetBlobName is not configured."),
    //         ContainerName = configuration["BlobTargetContainerName"] ?? throw new InvalidOperationException("BlobTargetContainerName is not configured."),
    //     };

    //     return (sourceInfo, targetInfo);
    // }

}
