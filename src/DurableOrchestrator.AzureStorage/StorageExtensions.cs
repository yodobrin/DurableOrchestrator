using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a set of extension methods for configuring Azure Storage services.
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Configures the Azure Blob Storage services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure Blob Storage services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure Blob Storage settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            return new BlobServiceClient(connectionString);
        });

        services.TryAddSingleton<StorageClientFactory>();

        return services;
    }

    /// <summary>
    /// Configures the Azure Table Storage services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure Table Storage services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure Table Storage settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddTableStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var connectionString = configuration.GetValue<string>("AzureWebJobsStorage");
            return new TableServiceClient(connectionString);
        });

        services.TryAddSingleton<StorageClientFactory>();

        return services;
    }
}
