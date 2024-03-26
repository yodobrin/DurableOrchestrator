using Azure.Security.KeyVault.Secrets;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureKeyVault;

/// <summary>
/// Defines a collection of activities for interacting with Azure Key Vault.
/// </summary>
/// <param name="client">The <see cref="SecretClient"/> instance used to interact with Azure Key Vault.</param>
/// <param name="logger">The logger for capturing telemetry and diagnostic information.</param>
[ActivitySource(nameof(KeyVaultActivities))]
public class KeyVaultActivities(
    SecretClient client,
    ILogger<KeyVaultActivities> logger)
    : BaseActivity(nameof(KeyVaultActivities))
{
    private const string DefaultSecretValue = "N/A";

    /// <summary>
    /// Retrieves a secret from Azure Key Vault based on the provided secret name.
    /// </summary>
    /// <remarks>
    /// If the secret is not found, a default value is returned instead.
    /// </remarks>
    /// <param name="input">The key vault secret request containing the name of the secret to retrieve.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>The value of the secret, or a default value if the secret is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="Exception">Thrown when an unhandled error occurs during the operation.</exception>
    [Function(nameof(GetSecretFromKeyVault))]
    public async Task<string> GetSecretFromKeyVault(
        [ActivityTrigger] KeyVaultSecretRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetSecretFromKeyVault), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetSecretFromKeyVault)}::{nameof(input)} is invalid. {validationResult}");
        }

        var secretName = input.SecretName;

        try
        {
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            logger.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            return secret.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
            return DefaultSecretValue;
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(GetSecretFromKeyVault), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Retrieves multiple secrets from Azure Key Vault based on a list of secret names.
    /// </summary>
    /// <remarks>
    /// Each secret's retrieval is attempted, and if not found, a default value is returned for it.
    /// </remarks>
    /// <param name="input">The key vault secret request containing the names of the secrets to retrieve.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>A list containing the values of the retrieved secrets, or default values for those not found.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="Exception">Thrown when an unhandled error occurs during the operation.</exception>
    [Function(nameof(GetSecretsFromKeyVault))]
    public async Task<List<string>> GetSecretsFromKeyVault(
        [ActivityTrigger] KeyVaultSecretRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetSecretsFromKeyVault), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetSecretFromKeyVault)}::{nameof(input)} is invalid. {validationResult}");
        }

        var secretNames = input.SecretNames!;
        var secretsValues = new List<string>();

        foreach (var secretName in secretNames)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                logger.LogWarning("One of the secret names is null or whitespace.");
                secretsValues.Add(DefaultSecretValue);
                continue;
            }

            try
            {
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                secretsValues.Add(secret.Value);
                logger.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                logger.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
                secretsValues.Add(DefaultSecretValue);
            }
            catch (Exception ex)
            {
                logger.LogError("Error retrieving secret {SecretName}: {Message}", secretName, ex.Message);
                secretsValues.Add(DefaultSecretValue);
            }
        }

        return secretsValues;
    }
}
