using System.Text.Json.Serialization;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.AzureKeyVault;

/// <summary>
/// Defines a model that represents information about a secret or secrets in Azure Key Vault.
/// </summary>
public class KeyVaultSecretInfo : IObservabilityContext
{
    /// <summary>
    /// Gets or sets the name of the secret to retrieve from Azure Key Vault.
    /// </summary>
    [JsonPropertyName("secretName")]
    public string? SecretName { get; set; }

    /// <summary>
    /// Gets or sets the names of the secrets to retrieve from Azure Key Vault.
    /// </summary>
    [JsonPropertyName("secretNames")]
    public IEnumerable<string>? SecretNames { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();
}
