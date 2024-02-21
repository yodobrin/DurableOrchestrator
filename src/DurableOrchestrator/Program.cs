using Azure.Identity;
using DurableOrchestrator.AI;
using DurableOrchestrator.KeyVault;
using DurableOrchestrator.Observability;
using DurableOrchestrator.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostContext, configuration) =>
    {
        configuration.SetBasePath(hostContext.HostingEnvironment.ContentRootPath)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();
    })
    .ConfigureServices((builder, services) =>
    {
        services.AddSingleton(_ =>
        {
            var azureCredentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                CredentialProcessTimeout = TimeSpan.FromSeconds(10)
            });

            return azureCredentials;
        });

        // No changes needed here for KeyVault and Observability
        services.AddObservability(builder);
        services.AddKeyVault();

        // Pass IConfiguration to AddBlobStorage where it is needed to read BlobSource and BlobTarget configurations
        services.AddBlobStorage(builder.Configuration);
        services.AddBlobStorageClients(builder.Configuration);
        services.AddTextAnalytics();
    })
    .Build();

host.Run();

/// <summary>
/// Defines the entry point for the application.
/// </summary>
public partial class Program;

