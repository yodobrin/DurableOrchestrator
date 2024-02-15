using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace DurableOrchestrator;

public static class KeyVaultActivities
{
    private const string DefaultSecretValue = "N/A";

    [Function(nameof(GetSecretFromKeyVault))]
    public static async Task<string> GetSecretFromKeyVault([ActivityTrigger] string secretName, FunctionContext executionContext)
    {
        var log = executionContext.GetLogger(nameof(GetSecretFromKeyVault));

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name must not be null or whitespace.");
        }

        var client = InitializeKeyVaultClient(log); // Throws InvalidOperationException if URL is not configured or the client cannot be instantiated

        try
        {
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            log.LogInformation($"Successfully retrieved secret: {secretName}");
            return secret.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            log.LogWarning($"Secret not found: {secretName}. Using default value.");
            return DefaultSecretValue; // Return default value if secret not found
        }
        catch (Exception ex)
        {
            log.LogError($"Error retrieving secret {secretName}: {ex.Message}");
            throw; // Rethrow exceptions other than not found
        }
    }

    [Function(nameof(GetMultipleSecretsFromKeyVault))]
    public static async Task<List<string>> GetMultipleSecretsFromKeyVault([ActivityTrigger] List<string> secretNames, FunctionContext executionContext)
    {
        var log = executionContext.GetLogger(nameof(GetMultipleSecretsFromKeyVault));

        var secretsValues = new List<string>();

        var client = InitializeKeyVaultClient(log); // Throws InvalidOperationException if URL is not configured or the client cannot be instantiated

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
                log.LogInformation($"Successfully retrieved secret: {secretName}");
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                log.LogWarning($"Secret not found: {secretName}. Defaulting to {DefaultSecretValue}");
                secretsValues.Add(DefaultSecretValue);
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected error retrieving secret {secretName}. Error: {ex.Message}. Defaulting to {DefaultSecretValue}");
                secretsValues.Add(DefaultSecretValue);
            }
        }

        return secretsValues;
    }

    private static SecretClient InitializeKeyVaultClient(ILogger log)
    {
        var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            log.LogError("Key Vault URL is not configured in the environment variables.");
            throw new InvalidOperationException("Key Vault URL is not configured.");
        }

        try
        {
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            return client;
        }
        catch (Exception ex)
        {
            // This catch block is for handling exceptions that might occur during the client's instantiation.
            // This could be due to issues with the DefaultAzureCredential not being able to obtain a token,
            // problems with the network, invalid Key Vault URL format, etc.
            log.LogError($"An error occurred while creating the KeyVault client: {ex.Message}");
            throw new InvalidOperationException("Failed to initialize Key Vault client.", ex);
        }
    }
}
