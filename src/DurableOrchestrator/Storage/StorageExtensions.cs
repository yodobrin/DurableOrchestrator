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
        // Register BlobServiceClientsWrapper with the DI container
        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            // Instantiate source and target BlobServiceClient using configuration
            var sourceStorageAccountName = configuration.GetValue<string>("BlobSourceStorageAccountName") ?? throw new InvalidOperationException("BlobSourceStorageAccountName is not configured.");
            var targetStorageAccountName = configuration.GetValue<string>("BlobTargetStorageAccountName") ?? throw new InvalidOperationException("BlobTargetStorageAccountName is not configured.");

            var sourceClient = new BlobServiceClient(new Uri($"https://{sourceStorageAccountName}.blob.core.windows.net"), credentials);
            var targetClient = new BlobServiceClient(new Uri($"https://{targetStorageAccountName}.blob.core.windows.net"), credentials);

            return new BlobServiceClientsWrapper(sourceClient, targetClient);
        });

        return services;
    }

}
