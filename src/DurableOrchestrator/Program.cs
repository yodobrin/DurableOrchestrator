using DurableOrchestrator.KeyVault;
using DurableOrchestrator.Observability;
using DurableOrchestrator.Storage;
using DurableOrchestrator.AI;
using Microsoft.Extensions.Configuration;
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

