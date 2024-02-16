using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.Storage;

internal static class StorageExtensions
{
    internal static IServiceCollection AddBlobStorage(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        return services;
    }
}
