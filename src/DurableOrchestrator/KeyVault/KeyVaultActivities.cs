using Azure.Security.KeyVault.Secrets;
using DurableOrchestrator.Activities;
using DurableOrchestrator.Models;
using DurableOrchestrator.Observability;

namespace DurableOrchestrator.KeyVault;

[ActivitySource(nameof(KeyVaultActivities))]
public class KeyVaultActivities(SecretClient client, ILogger<KeyVaultActivities> log) : BaseActivity(nameof(KeyVaultActivities))
{
    private const string DefaultSecretValue = "N/A";

    /// <summary>
    /// Retrieves a single secret from Azure Key Vault. If the secret is not found, a default value is returned. This method logs the outcome of the operation and handles exceptions by logging them and rethrowing.
    /// </summary>
    /// <param name="input">The activity input containing the name of the secret to retrieve.</param>
    /// <param name="executionContext">The function execution context for logging and other execution-related functionalities.</param>
    /// <returns>The value of the secret, or a default value if the secret is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when the secret name is null or whitespace.</exception>
    [Function(nameof(GetSecretFromKeyVault))]
    public async Task<string> GetSecretFromKeyVault(
        [ActivityTrigger] KeyVaultRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetSecretFromKeyVault), input);

        var secretName = input.SecretName;

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name must not be null or whitespace.");
        }

        try
        {
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            log.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            return secret.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            log.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
            return DefaultSecretValue; // Return default value if secret not found
        }
        catch (Exception ex)
        {
            log.LogError("Error retrieving secret {SecretName}: {Message}", secretName, ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw; // Rethrow exceptions other than not found
        }
    }

    /// <summary>
    /// Retrieves multiple secrets from Azure Key Vault based on a list of secret names. Each secret's retrieval is attempted, and if not found, a default value is returned for it. The method logs the outcome of each attempt.
    /// </summary>
    /// <param name="input">The activity input containing a list of secret names to retrieve.</param>
    /// <param name="executionContext">The function execution context for logging and other execution-related functionalities.</param>
    /// <returns>A list containing the values of the retrieved secrets, or default values for those not found.</returns>
    [Function(nameof(GetMultipleSecretsFromKeyVault))]
    public async Task<List<string>> GetMultipleSecretsFromKeyVault(
        [ActivityTrigger] KeyVaultRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetMultipleSecretsFromKeyVault), input);

        var secretNames = input.SecretNames ?? throw new ArgumentException("Secret names must not be null.");
        var secretsValues = new List<string>();

        foreach (var secretName in secretNames)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                log.LogWarning("One of the secret names is null or whitespace.");
                secretsValues.Add(DefaultSecretValue);
                continue;
            }

            try
            {
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                secretsValues.Add(secret.Value);
                log.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                log.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
                secretsValues.Add(DefaultSecretValue);
            }
            catch (Exception ex)
            {
                log.LogError("Error retrieving secret {SecretName}: {Message}", secretName, ex.Message);
                secretsValues.Add(DefaultSecretValue);
            }
        }

        return secretsValues;
    }
}
