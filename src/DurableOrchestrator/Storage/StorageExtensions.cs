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

        // Retrieve and register the BlobServiceClient for the source blob storage
        var blobSourceInfo = configuration.GetSection("Values:BlobSource").Get<BlobStorageInfo>();
        if (blobSourceInfo != null)
        {
            services.AddSingleton(sp => GetBlobServiceClient(blobSourceInfo));
        }
        else
        {
            throw new InvalidOperationException("BlobSource configuration is missing or invalid.");
        }

        // Retrieve and register the BlobServiceClient for the target blob storage
        var blobTargetInfo = configuration.GetSection("Values:BlobTarget").Get<BlobStorageInfo>();
        if (blobTargetInfo != null)
        {
            services.AddSingleton(sp => GetBlobServiceClient(blobTargetInfo));
        }
        else
        {
            throw new InvalidOperationException("BlobTarget configuration is missing or invalid.");
        }

        return services;
    }

    private static BlobServiceClient GetBlobServiceClient(BlobStorageInfo input)
    {
        if (string.IsNullOrWhiteSpace(input.BlobUri))
        {
            if (string.IsNullOrWhiteSpace(input.StorageAccountName) || string.IsNullOrWhiteSpace(input.ContainerName))
            {
                throw new ArgumentException("Container name or Storage Account name is not provided, and no BlobUri is specified.");
            }

            // Assuming DefaultAzureCredential will pick up the necessary authentication details from the environment
            return new BlobServiceClient(new Uri($"https://{input.StorageAccountName}.blob.core.windows.net"), new DefaultAzureCredential());
        }
        else
        {
            return new BlobServiceClient(new Uri(input.BlobUri), new DefaultAzureCredential());
        }
    }

}
