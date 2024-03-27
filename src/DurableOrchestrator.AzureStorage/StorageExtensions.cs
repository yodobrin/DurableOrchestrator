using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a set of extension methods for configuring Azure Storage services.
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Configures the Azure Storage services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure Storage services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure Storage settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        services.AddSingleton<BlobServiceClientFactory>();

        return services;
    }
}
