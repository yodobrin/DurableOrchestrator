using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureKeyVault;

/// <summary>
/// Defines the settings for configuring Azure Key Vault.
/// </summary>
/// <param name="keyVaultUrl">The URL of the Azure Key Vault.</param>
public class KeyVaultSettings(string keyVaultUrl)
{
    /// <summary>
    /// The configuration key for the Azure Key Vault URL.
    /// </summary>
    public const string KeyVaultUrlConfigKey = "KEY_VAULT_URL";

    /// <summary>
    /// Gets the name of the source Azure Storage account.
    /// </summary>
    public string KeyVaultUrl { get; init; } = keyVaultUrl;

    /// <summary>
    /// Creates a new instance of the <see cref="KeyVaultSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="KeyVaultSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the KeyVaultUrl is not configured.</exception>
    public static KeyVaultSettings FromConfiguration(IConfiguration configuration)
    {
        var keyVaultUrl = configuration.GetValue<string>(KeyVaultUrlConfigKey) ??
                          throw new InvalidOperationException(
                              "KeyVaultUrl is not configured.");

        return new KeyVaultSettings(keyVaultUrl);
    }
}
