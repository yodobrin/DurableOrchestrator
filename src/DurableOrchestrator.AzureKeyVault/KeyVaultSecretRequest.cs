using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureKeyVault;

/// <summary>
/// Defines a model that represents information about a secret or secrets in Azure Key Vault.
/// </summary>
public class KeyVaultSecretRequest : IWorkflowRequest
{
    /// <summary>
    /// Gets or sets the name of the secret to retrieve.
    /// </summary>
    [JsonPropertyName("secretName")]
    public string? SecretName { get; set; }

    /// <summary>
    /// Gets or sets the names of the secrets to retrieve.
    /// </summary>
    [JsonPropertyName("secretNames")]
    public IEnumerable<string>? SecretNames { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(SecretName) && (SecretNames == null || !SecretNames.Any()))
        {
            result.AddErrorMessage($"{nameof(SecretName)} or {nameof(SecretNames)} must be provided.");
        }

        return result;
    }
}
