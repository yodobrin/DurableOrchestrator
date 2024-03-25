using Azure.Identity;
using DurableOrchestrator.AI;
using DurableOrchestrator.KeyVault;
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
            var credentialOpts = new DefaultAzureCredentialOptions
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
            };

            var managedIdentityClientId = builder.Configuration.GetValue<string>("MANAGED_IDENTITY_CLIENT_ID");
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                credentialOpts.ManagedIdentityClientId = managedIdentityClientId;
            }

            return new DefaultAzureCredential(credentialOpts);
        });

        // No changes needed here for KeyVault and Observability
        services.AddObservability(builder);
        services.AddKeyVault();

        // Pass IConfiguration to AddBlobStorage where it is needed to read BlobSource and BlobTarget configurations
        services.AddBlobStorage(builder.Configuration);
        // Required for most activities, reads and write to blob storage
        services.AddBlobStorageClients(builder.Configuration);
        // Required if Text Analytics is used
        services.AddTextAnalytics();
        // Required if Document Intelligence is used
        services.AddDocumentIntelligence();
    })
    .Build();

host.Run();

/// <summary>
/// Defines the entry point for the application.
/// </summary>
public partial class Program;

