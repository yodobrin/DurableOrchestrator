using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.KeyVault;

internal static class KeyVaultExtensions
{
    internal static IServiceCollection AddKeyVault(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");

            if (string.IsNullOrWhiteSpace(keyVaultUrl))
            {
                throw new InvalidOperationException("Key Vault URL is not configured.");
            }

            return new SecretClient(new Uri(keyVaultUrl), credentials);
        });

        return services;
    }
}
