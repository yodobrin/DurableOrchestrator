using DurableOrchestrator.KeyVault;
using DurableOrchestrator.Observability;
using DurableOrchestrator.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((_, configuration) =>
    {
        configuration.AddEnvironmentVariables();
    })
    .ConfigureServices((builder, services) =>
    {
        services.AddObservability(builder);
        services.AddKeyVault();
        services.AddBlobStorage();
    })
    .Build();

host.Run();

/// <summary>
/// Defines the entry point for the application.
/// </summary>
public partial class Program;
