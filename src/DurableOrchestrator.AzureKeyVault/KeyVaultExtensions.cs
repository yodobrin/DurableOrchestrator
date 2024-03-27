using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureKeyVault;

/// <summary>
/// Defines a set of extension methods for configuring Azure Key Vault services.
/// </summary>
public static class KeyVaultExtensions
{
    /// <summary>
    /// Configures the Azure Key Vault services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure Key Vault services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure Key Vault settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = KeyVaultSettings.FromConfiguration(configuration);
        services.AddScoped(_ => settings);

        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            return new SecretClient(new Uri(settings.KeyVaultUrl), credentials);
        });

        return services;
    }
}
