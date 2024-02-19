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

}
